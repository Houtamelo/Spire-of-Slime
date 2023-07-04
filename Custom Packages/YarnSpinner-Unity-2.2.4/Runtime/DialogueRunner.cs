/*

The MIT License (MIT)

Copyright (c) 2015-2017 Secret Lab Pty. Ltd. and Yarn Spinner contributors.

Permission is hereby granted, free of charge, to any person obtaining a
copy of this software and associated documentation files (the "Software"),
to deal in the Software without restriction, including without limitation
the rights to use, copy, modify, merge, publish, distribute, sublicense,
and/or sell copies of the Software, and to permit persons to whom the
Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
DEALINGS IN THE SOFTWARE.

*/

using Core.Visual_Novel.Scripts;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Events;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Linq;

namespace Yarn.Unity
{
    /// <summary>
    /// The DialogueRunner component acts as the interface between your game and
    /// Yarn Spinner.
    /// </summary>
    [AddComponentMenu("Scripts/Yarn Spinner/Dialogue Runner"), HelpURL("https://yarnspinner.dev/docs/unity/components/dialogue-runner/")]
    public class DialogueRunner : MonoBehaviour
    {
        public enum CommandDispatchResult 
        {
            Success,
            Failed,
            NotFound,
        }
        
        [UnityEngine.Serialization.FormerlySerializedAs("yarnProgram")]
        public YarnProject yarnProject;
        
        [UnityEngine.Serialization.FormerlySerializedAs("variableStorage")]
        [SerializeField] public VariableStorageBehaviour _variableStorage;

        /// <inheritdoc cref="_variableStorage"/>
        public VariableStorageBehaviour VariableStorage
        {
            get => _variableStorage; 
            set
            {
                _variableStorage = value;
                if (_dialogue != null)
                {
                    _dialogue.VariableStorage = value;
                }
            }
        }

        private YarnRoutineWrapper _currentRoutine;
        public bool IsRunningIEnumeratorCommand => _currentRoutine is { Running: true };

        public DialogueViewBase[] dialogueViews;
        public string startNode = Dialogue.DefaultStartNodeName;
        public bool startAutomatically = true;
        public bool runSelectedOptionAsLine;
        public LineProviderBehaviour lineProvider;
        [Tooltip("If true, will print Debug.Log messages every time it enters a node, and other frequent events")]
        public bool verboseLogging = true;
        public bool IsDialogueRunning { get; set; }


        [Serializable] public class StringUnityEvent : UnityEvent<string> { }
        public StringUnityEvent onNodeStart;
        public StringUnityEvent onNodeComplete;
        public UnityEvent onDialogueComplete;

        /// <summary>
        /// A <see cref="StringUnityEvent"/> that is called when a <see
        /// cref="Command"/> is received.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Use this method to dispatch a command to other parts of your game.
        /// This method is only called if the <see cref="Command"/> has not been
        /// handled by a command handler that has been added to the <see
        /// cref="DialogueRunner"/>, or by a method on a <see
        /// cref="MonoBehaviour"/> in the scene with the attribute <see
        /// cref="YarnCommandAttribute"/>.
        /// </para>
        /// <para style="hint">
        /// When a command is delivered in this way, the <see
        /// cref="DialogueRunner"/> will not pause execution. If you want a
        /// command to make the DialogueRunner pause execution, see <see
        /// cref="AddCommandHandler(string, CommandHandler)"/>.
        /// </para>
        /// <para>
        /// This method receives the full text of the command, as it appears
        /// between the <c>&lt;&lt;</c> and <c>&gt;&gt;</c> markers.
        /// </para>
        /// </remarks>
        /// <seealso cref="AddCommandHandler(string, CommandHandler)"/>
        /// <seealso cref="AddCommandHandler(string, CommandHandler)"/>
        /// <seealso cref="YarnCommandAttribute"/>
        public StringUnityEvent onCommand;

        /// <summary>
        /// Gets the name of the current node that is being run.
        /// </summary>
        /// <seealso cref="Dialogue.currentNode"/>
        public string CurrentNodeName => Dialogue.CurrentNode;

        /// <summary>
        /// Gets the underlying <see cref="Dialogue"/> object that runs the
        /// Yarn code.
        /// </summary>
        public Dialogue Dialogue => _dialogue ?? (_dialogue = CreateDialogueInstance());

        /// <summary>
        /// A flag used to detect if an options handler attempts to set the
        /// selected option on the same frame that options were provided.
        /// </summary>
        /// <remarks>
        /// This field is set to false by <see
        /// cref="HandleOptions(OptionSet)"/> immediately before calling
        /// <see cref="DialogueViewBase.RunOptions(DialogueOption[],
        /// Action{int})"/> on all objects in <see cref="dialogueViews"/>,
        /// and set to true immediately after. If a call to <see
        /// cref="DialogueViewBase.RunOptions(DialogueOption[],
        /// Action{int})"/> calls its completion hander on the same frame,
        /// an error is generated.
        /// </remarks>
        private bool IsOptionSelectionAllowed = false;

        /// <summary>
        /// Replaces this DialogueRunner's yarn project with the provided
        /// project.
        /// </summary>
        public void SetProject(YarnProject newProject)
        {
            // Load all of the commands and functions from the assemblies that
            // this project wants to load from.
            ActionManager.AddActionsFromAssemblies(newProject.searchAssembliesForActions);

            // Register any new functions that we found as part of doing this.
            ActionManager.RegisterFunctions(Dialogue.Library);

            Dialogue.SetProgram(newProject.Program);

            if (lineProvider != null) {
                lineProvider.YarnProject = newProject;
            }
        }

        /// <summary>
        /// Loads any initial variables declared in the program and loads that variable with its default declaration value into the variable storage.
        /// Any variable that is already in the storage will be skipped, the assumption is that this means the value has been overridden at some point and shouldn't be otherwise touched.
        /// Can force an override of the existing values with the default if that is desired.
        /// </summary>
        public void SetInitialVariables(bool overrideExistingValues = false)
        {
            if (yarnProject == null)
            {
                Debug.LogError("Unable to set default values, there is no project set");
                return;
            }

            // grabbing all the initial values from the program and inserting them into the storage
            // we first need to make sure that the value isn't already set in the storage
            var values = yarnProject.Program.InitialValues;
            foreach (var pair in values)
            {
                if (!overrideExistingValues && VariableStorage.Contains(pair.Key))
                {
                    continue;
                }
                var value = pair.Value;
                switch (value.ValueCase)
                {
                    case Operand.ValueOneofCase.StringValue:
                    {
                        VariableStorage.SetValue(pair.Key, value.StringValue);
                        break;
                    }
                    case Operand.ValueOneofCase.BoolValue:
                    {
                        VariableStorage.SetValue(pair.Key, value.BoolValue);
                        break;
                    }
                    case Operand.ValueOneofCase.FloatValue:
                    {
                        VariableStorage.SetValue(pair.Key, value.FloatValue);
                        break;
                    }
                    default:
                    {
                        Debug.LogWarning($"{pair.Key} is of an invalid type: {value.ValueCase}");
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Start the dialogue from a specific node.
        /// </summary>
        /// <param name="startNode">The name of the node to start running
        /// from.</param>
        public void StartDialogue(string startNode)
        {
            // If the dialogue is currently executing instructions, then
            // calling ContinueDialogue() at the end of this method will
            // cause confusing results. Report an error and stop here.
            if (Dialogue.IsActive) 
            {
                Debug.LogError($"Can't start dialogue from node {startNode}: the dialogue is currently in the middle of running. Stop the dialogue first.");
                return;
            }

            // Stop any processes that might be running already
            foreach (var dialogueView in dialogueViews)
            {
                if (dialogueView == null) 
                {
                    continue;
                }

                dialogueView.StopAllCoroutines();
            }

            // Get it going

            // Mark that we're in conversation.
            IsDialogueRunning = true;

            // Signal that we're starting up.
            foreach (var dialogueView in dialogueViews)
            {
                if (dialogueView == null)
                {
                    continue;
                }

                dialogueView.DialogueStarted();
            }

            // Request that the dialogue select the current node. This
            // will prepare the dialogue for running; as a side effect,
            // our prepareForLines delegate may be called.
            Dialogue.SetNode(startNode);

            if (lineProvider.LinesAvailable == false)
            {
                // The line provider isn't ready to give us our lines
                // yet. We need to start a coroutine that waits for
                // them to finish loading, and then runs the dialogue.
                StartCoroutine(ContinueDialogueWhenLinesAvailable());
            }
            else
            {
                ContinueDialogue();
            }
        }

        private IEnumerator ContinueDialogueWhenLinesAvailable()
        {
            // Wait until lineProvider.LinesAvailable becomes true
            while (lineProvider.LinesAvailable == false)
            {
                yield return null;
            }

            // And then run our dialogue.
            ContinueDialogue();
        }

        /// <summary>
        /// Starts running the dialogue again.
        /// </summary>
        /// <remarks>
        /// If <paramref name="nodeName"/> is null, the node specified by
        /// <see cref="startNode"/> is attempted, followed the currently
        /// running node. If none of these options are available, an <see
        /// cref="ArgumentNullException"/> is thrown.
        /// </remarks>
        /// <exception cref="ArgumentNullException">Thrown when a node to
        /// restart the dialogue from cannot be found.</exception>
        [Obsolete("Use " + nameof(StartDialogue) + "(nodeName) instead.")]
        public void ResetDialogue(string nodeName = null)
        {
            nodeName = nodeName ?? startNode ?? CurrentNodeName ?? throw new ArgumentNullException($"Cannot reset dialogue: couldn't figure out a node to restart the dialogue from.");

            StartDialogue(nodeName);
        }

        /// <summary>
        /// Unloads all nodes from the <see cref="Dialogue"/>.
        /// </summary>
        public void Clear()
        {
            Assert.IsFalse(IsDialogueRunning, "You cannot clear the dialogue system while a dialogue is running.");
            Dialogue.UnloadAll();
        }

        /// <summary>
        /// Stops the <see cref="Dialogue"/>.
        /// </summary>
        public void Stop()
        {
            IsDialogueRunning = false;
            Dialogue.Stop();
        }

        /// <summary>
        /// Returns `true` when a node named `nodeName` has been loaded.
        /// </summary>
        /// <param name="nodeName">The name of the node.</param>
        /// <returns>`true` if the node is loaded, `false`
        /// otherwise/</returns>
        public bool NodeExists(string nodeName) => Dialogue.NodeExists(nodeName);

        /// <summary>
        /// Returns the collection of tags that the node associated with
        /// the node named `nodeName`.
        /// </summary>
        /// <param name="nodeName">The name of the node.</param>
        /// <returns>The collection of tags associated with the node, or
        /// `null` if no node with that name exists.</returns>
        public IEnumerable<string> GetTagsForNode(String nodeName) => Dialogue.GetTagsForNode(nodeName);

#region CommandsAndFunctions
        /// <summary>
        /// Adds a command handler. Dialogue will pause execution after the
        /// command is called.
        /// </summary>
        /// <remarks>
        /// <para>When this command handler has been added, it can be called
        /// from your Yarn scripts like so:</para>
        ///
        /// <code lang="yarn">
        /// &lt;&lt;commandName param1 param2&gt;&gt;
        /// </code>
        ///
        /// <para>If <paramref name="handler"/> is a method that returns a <see
        /// cref="Coroutine"/>, when the command is run, the <see
        /// cref="DialogueRunner"/> will wait for the returned coroutine to stop
        /// before delivering any more content.</para>
        /// </remarks>
        /// <param name="commandName">The name of the command.</param>
        /// <param name="handler">The <see cref="CommandHandler"/> that will be
        /// invoked when the command is called.</param>
        public void AddCommandHandler(string commandName, Delegate handler)
        {
            if (commandHandlers.ContainsKey(commandName))
            {
                Debug.LogError($"Cannot add a command handler for {commandName}: one already exists");
                return;
            }
            commandHandlers.Add(commandName, handler);
        }

        /*/// <inheritdoc cref="AddCommandHandler(string, Delegate)"/>
        public void AddCommandHandler(string commandName, Func<Coroutine> handler)
        {
            AddCommandHandler(commandName, (Delegate)handler);
        }

        /// <inheritdoc cref="AddCommandHandler(string, Delegate)"/>
        public void AddCommandHandler<T1>(string commandName, Func<T1, Coroutine> handler)
        {
            AddCommandHandler(commandName, (Delegate)handler);
        }

        /// <inheritdoc cref="AddCommandHandler(string, Delegate)"/>
        public void AddCommandHandler<T1, T2>(string commandName, Func<T1, T2, Coroutine> handler)
        {
            AddCommandHandler(commandName, (Delegate)handler);
        }

        /// <inheritdoc cref="AddCommandHandler(string, Delegate)"/>
        public void AddCommandHandler<T1, T2, T3>(string commandName, Func<T1, T2, T3, Coroutine> handler)
        {
            AddCommandHandler(commandName, (Delegate)handler);
        }

        /// <inheritdoc cref="AddCommandHandler(string, Delegate)"/>
        public void AddCommandHandler<T1, T2, T3, T4>(string commandName, Func<T1, T2, T3, T4, Coroutine> handler)
        {
            AddCommandHandler(commandName, (Delegate)handler);
        }

        /// <inheritdoc cref="AddCommandHandler(string, Delegate)"/>
        public void AddCommandHandler<T1, T2, T3, T4, T5>(string commandName, Func<T1, T2, T3, T4, T5, Coroutine> handler)
        {
            AddCommandHandler(commandName, (Delegate)handler);
        }

        /// <inheritdoc cref="AddCommandHandler(string, Delegate)"/>
        public void AddCommandHandler<T1, T2, T3, T4, T5, T6>(string commandName, Func<T1, T2, T3, T4, T5, T6, Coroutine> handler)
        {
            AddCommandHandler(commandName, (Delegate)handler);
        }

        /// <inheritdoc cref="AddCommandHandler(string, Delegate)"/>
        public void AddCommandHandler(string commandName, Action handler)
        {
            AddCommandHandler(commandName, (Delegate)handler);
        }

        /// <inheritdoc cref="AddCommandHandler(string, Delegate)"/>
        public void AddCommandHandler<T1>(string commandName, Action<T1> handler)
        {
            AddCommandHandler(commandName, (Delegate)handler);
        }

        /// <inheritdoc cref="AddCommandHandler(string, Delegate)"/>
        public void AddCommandHandler<T1, T2>(string commandName, Action<T1, T2> handler)
        {
            AddCommandHandler(commandName, (Delegate)handler);
        }

        /// <inheritdoc cref="AddCommandHandler(string, Delegate)"/>
        public void AddCommandHandler<T1, T2, T3>(string commandName, Action<T1, T2, T3> handler)
        {
            AddCommandHandler(commandName, (Delegate)handler);
        }

        /// <inheritdoc cref="AddCommandHandler(string, Delegate)"/>
        public void AddCommandHandler<T1, T2, T3, T4>(string commandName, Action<T1, T2, T3, T4> handler)
        {
            AddCommandHandler(commandName, (Delegate)handler);
        }

        /// <inheritdoc cref="AddCommandHandler(string, Delegate)"/>
        public void AddCommandHandler<T1, T2, T3, T4, T5>(string commandName, Action<T1, T2, T3, T4, T5> handler)
        {
            AddCommandHandler(commandName, (Delegate)handler);
        }

        /// <inheritdoc cref="AddCommandHandler(string, Delegate)"/>
        public void AddCommandHandler<T1, T2, T3, T4, T5, T6>(string commandName, Action<T1, T2, T3, T4, T5, T6> handler)
        {
            AddCommandHandler(commandName, (Delegate)handler);
        }*/

        /// <summary>
        /// Removes a command handler.
        /// </summary>
        /// <param name="commandName">The name of the command to
        /// remove.</param>
        public void RemoveCommandHandler(string commandName)
        {
            commandHandlers.Remove(commandName);
        }

        /// <summary>
        /// Add a new function that returns a value, so that it can be
        /// called from Yarn scripts.
        /// </summary>
        /// <remarks>
        /// <para>When this function has been registered, it can be called from
        /// your Yarn scripts like so:</para>
        ///
        /// <code lang="yarn">
        /// &lt;&lt;if myFunction(1, 2) == true&gt;&gt;
        ///     myFunction returned true!
        /// &lt;&lt;endif&gt;&gt;
        /// </code>
        ///
        /// <para>The <c>call</c> command can also be used to invoke the function:</para>
        ///
        /// <code lang="yarn">
        /// &lt;&lt;call myFunction(1, 2)&gt;&gt;
        /// </code>
        /// </remarks>
        /// <param name="implementation">The <see cref="Delegate"/> that
        /// should be invoked when this function is called.</param>
        /// <seealso cref="Library"/>
        public void AddFunction(string name, Delegate implementation)
        {
            if (Dialogue.Library.FunctionExists(name))
            {
                Debug.LogError($"Cannot add function {name}: one already exists");
                return;
            }

            Dialogue.Library.RegisterFunction(name, implementation);
        }

        /// <inheritdoc cref="AddFunction(string, Delegate)" />
        /// <typeparam name="TResult">The type of the value that the function should return.</typeparam>
        public void AddFunction<TResult>(string name, Func<TResult> implementation)
        {
            AddFunction(name, (Delegate)implementation);
        }

        /// <inheritdoc cref="AddFunction{TResult}(string, Func{TResult})" />
        /// <typeparam name="T1">The type of the first parameter to the function.</typeparam>
        public void AddFunction<TResult, T1>(string name, Func<TResult, T1> implementation)
        {
            AddFunction(name, (Delegate)implementation);
        }

        /// <inheritdoc cref="AddFunction{TResult,T1}(string, Func{TResult,T1})" />
        /// <typeparam name="T2">The type of the second parameter to the function.</typeparam>
        public void AddFunction<TResult, T1, T2>(string name, Func<TResult, T1, T2> implementation)
        {
            AddFunction(name, (Delegate)implementation);
        }

        /// <inheritdoc cref="AddFunction{TResult,T1,T2}(string, Func{TResult,T1,T2})" />
        /// <typeparam name="T3">The type of the third parameter to the function.</typeparam>
        public void AddFunction<TResult, T1, T2, T3>(string name, Func<TResult, T1, T2, T3> implementation)
        {
            AddFunction(name, (Delegate)implementation);
        }

        /// <inheritdoc cref="AddFunction{TResult,T1,T2,T3}(string, Func{TResult,T1,T2,T3})" />
        /// <typeparam name="T4">The type of the fourth parameter to the function.</typeparam>
        public void AddFunction<TResult, T1, T2, T3, T4>(string name, Func<TResult, T1, T2, T3, T4> implementation)
        {
            AddFunction(name, (Delegate)implementation);
        }

        /// <inheritdoc cref="AddFunction{TResult,T1,T2,T3,T4}(string, Func{TResult,T1,T2,T3,T4})" />
        /// <typeparam name="T5">The type of the fifth parameter to the function.</typeparam>
        public void AddFunction<TResult, T1, T2, T3, T4, T5>(string name, Func<TResult, T1, T2, T3, T4, T5> implementation)
        {
            AddFunction(name, (Delegate)implementation);
        }

        /// <inheritdoc cref="AddFunction{TResult,T1,T2,T3,T4,T5}(string, Func{TResult,T1,T2,T3,T4,T5})" />
        /// <typeparam name="T6">The type of the sixth parameter to the function.</typeparam>
        public void AddFunction<TResult, T1, T2, T3, T4, T5, T6>(string name, Func<TResult, T1, T2, T3, T4, T5, T6> implementation)
        {
            AddFunction(name, (Delegate)implementation);
        }

        /// <summary>
        /// Remove a registered function.
        /// </summary>
        /// <remarks>
        /// After a function has been removed, it cannot be called from
        /// Yarn scripts.
        /// </remarks>
        /// <param name="name">The name of the function to remove.</param>
        /// <seealso cref="AddFunction{TResult}(string, Func{TResult})"/>
        public void RemoveFunction(string name) => Dialogue.Library.DeregisterFunction(name);
#endregion

#region Private Properties/Variables/Procedures

        /// <summary>
        /// The <see cref="LocalizedLine"/> currently being displayed on
        /// the dialogue views.
        /// </summary>
        public LocalizedLine CurrentLine { get; private set; }

        /// <summary>
        ///  The collection of dialogue views that are currently either
        ///  delivering a line, or dismissing a line from being on screen.
        /// </summary>
        private readonly HashSet<DialogueViewBase> ActiveDialogueViews = new HashSet<DialogueViewBase>();

        Action<int> selectAction;

        /// Maps the names of commands to action delegates.
        Dictionary<string, Delegate> commandHandlers = new Dictionary<string, Delegate>();

        /// <summary>
        /// The underlying object that executes Yarn instructions
        /// and provides lines, options and commands.
        /// </summary>
        /// <remarks>
        /// Automatically created on first access.
        /// </remarks>
        private Dialogue _dialogue;

        /// <summary>
        /// The current set of options that we're presenting.
        /// </summary>
        /// <remarks>
        /// This value is <see langword="null"/> when the <see
        /// cref="DialogueRunner"/> is not currently presenting options.
        /// </remarks>
        private OptionSet currentOptions;

        void Awake()
        {
            
            if (dialogueViews.Length == 0)
            {
                Debug.LogWarning($"Dialogue Runner doesn't have any dialogue views set up. No lines or options will be visible.");
            }

            foreach (var view in dialogueViews)
            {
                if (view == null)
                {
                    continue;
                }
                view.requestInterrupt = OnViewRequestedInterrupt;
            }

            if (yarnProject != null)
            {
                if (Dialogue.IsActive)
                {
                    Debug.LogError($"DialogueRunner wanted to load a Yarn Project in its Start method, but the Dialogue was already running one. The Dialogue Runner may not behave as you expect.");
                }

                // Load this new Yarn Project.
                SetProject(yarnProject);
            }

            if (lineProvider == null)
            {
                // If we don't have a line provider, create a
                // TextLineProvider and make it use that.

                if (yarnProject == null || yarnProject.localizationType == LocalizationType.YarnInternal) {
                    // Create the temporary line provider and the line database
                    lineProvider = gameObject.AddComponent<TextLineProvider>();
                    lineProvider.YarnProject = yarnProject;

                    // Let the user know what we're doing.
                    if (verboseLogging)
                    {
                        Debug.Log($"Dialogue Runner has no LineProvider; creating a {nameof(TextLineProvider)}.", this);
                    }
                } else {
#if USE_UNITY_LOCALIZATION && YARN_ENABLE_EXPERIMENTAL_FEATURES
                    Debug.LogError($"The Yarn Project \"{yarnProject.name}\" uses the Unity Localization system. Please add a {nameof(UnityLocalization.UnityLocalisedLineProvider)} component.");
#else
                    Debug.LogError($"The Yarn Project \"{yarnProject.name}\" uses the Unity Localization system, but the Unity Localization system is not currently installed. Please install it.");
#endif
                }
            }
        }

        /// <summary>
        /// Prepares the Dialogue Runner for start.
        /// </summary>
        /// <remarks>If <see cref="startAutomatically"/> is <see langword="true"/>, the Dialogue Runner will start.</remarks>
        private void Start()
        {
            if (yarnProject != null && startAutomatically)
            {
                StartDialogue(startNode);
            }
        }

        Dialogue CreateDialogueInstance()
        {
            if (VariableStorage == null)
            {
                // If we don't have a variable storage, create an
                // InMemoryVariableStorage and make it use that.

                VariableStorage = gameObject.AddComponent<InMemoryVariableStorage>();

                // Let the user know what we're doing.
                if (verboseLogging)
                {
                    Debug.Log($"Dialogue Runner has no Variable Storage; creating a {nameof(InMemoryVariableStorage)}", this);
                }
            }

            // Create the main Dialogue runner, and pass our
            // variableStorage to it
            var dialogue = new Dialogue(VariableStorage)
            {
                // Set up the logging system.
                LogDebugMessage = delegate (string message)
                {
                    if (verboseLogging)
                    {
                        Debug.Log(message);
                    }
                },
                LogErrorMessage = delegate (string message)
                {
                    Debug.LogError(message);
                },

                LineHandler = HandleLine,
                CommandHandler = HandleCommand,
                OptionsHandler = HandleOptions,
                NodeStartHandler = (node) =>
                {
                    onNodeStart?.Invoke(node);
                },
                NodeCompleteHandler = (node) =>
                {
                    onNodeComplete?.Invoke(node);
                },
                DialogueCompleteHandler = HandleDialogueComplete,
                PrepareForLinesHandler = PrepareForLines
            };

            selectAction = SelectedOption;
            return dialogue;
        }

        void HandleOptions(OptionSet options)
        {
            currentOptions = options;

            DialogueOption[] optionSet = new DialogueOption[options.Options.Length];
            for (int i = 0; i < options.Options.Length; i++)
            {
                // Localize the line associated with the option
                var localisedLine = lineProvider.GetLocalizedLine(options.Options[i].Line);
                var text = Dialogue.ExpandSubstitutions(localisedLine.RawText, options.Options[i].Line.Substitutions);

                Dialogue.LanguageCode = lineProvider.LocaleCode;

                try
                {
                    localisedLine.Text = Dialogue.ParseMarkup(text);
                }
                catch (Markup.MarkupParseException e)
                {
                    // Parsing the markup failed. We'll log a warning, and
                    // produce a markup result that just contains the raw text.
                    Debug.LogWarning($"Failed to parse markup in \"{text}\": {e.Message}");
                    localisedLine.Text = new Markup.MarkupParseResult
                    {
                        Text = text,
                        Attributes = new List<Markup.MarkupAttribute>()
                    };
                }

                optionSet[i] = new DialogueOption
                {
                    TextID = options.Options[i].Line.ID,
                    DialogueOptionID = options.Options[i].ID,
                    Line = localisedLine,
                    IsAvailable = options.Options[i].IsAvailable,
                };
            }
            
            // Don't allow selecting options on the same frame that we
            // provide them
            IsOptionSelectionAllowed = false;

            foreach (var dialogueView in dialogueViews)
            {
                if (dialogueView == null)
                    continue;

                dialogueView.RunOptions(optionSet, selectAction);
            }

            IsOptionSelectionAllowed = true;
        }

        void HandleDialogueComplete()
        {
            IsDialogueRunning = false;
            foreach (var dialogueView in dialogueViews)
            {
                if (dialogueView == null) continue;

                dialogueView.DialogueComplete();
            }
            onDialogueComplete.Invoke();
        }

        void HandleCommand(Command command)
        {
            CommandDispatchResult dispatchResult;

            // Try looking in the command handlers first
            dispatchResult = DispatchCommandToRegisteredHandlers(command, ContinueDialogue);

            if (dispatchResult != CommandDispatchResult.NotFound)
            {
                // We found the command! We don't need to keep looking. (It may
                // have succeeded or failed; if it failed, it logged something
                // to the console or otherwise communicated to the developer
                // that something went wrong. Either way, we don't need to do
                // anything more here.)
                return;
            }

            // We didn't find it in the comand handlers. Try looking in the
            // game objects. If it is, continue dialogue.
            dispatchResult = DispatchCommandToGameObject(command, ContinueDialogue);

            if (dispatchResult != CommandDispatchResult.NotFound)
            {
                // As before: we found a handler for this command, so we stop
                // looking.
                return;
            }

            // We didn't find a method in our C# code to invoke. Try invoking on
            // the publicly exposed UnityEvent.
            //
            // We can only do this if our onCommand event is not null and would
            // do something if we invoked it, so test this now.
            if (onCommand != null && onCommand.GetPersistentEventCount() > 0) {
                // We can invoke the event!
                onCommand.Invoke(command.Text);
            } else {
                // We're out of ways to handle this command! Log this as an
                // error.
                Debug.LogError($"No Command <<{command.Text}>> was found. Did you remember to use the YarnCommand attribute or AddCommandHandler() function in C#?");
            }

            // Whether we successfully handled it via the Unity Event or not,
            // attempting to handle the command this way doesn't interrupt the
            // dialogue, so we'll continue it now.
            ContinueDialogue();
        }

        /// <summary>
        /// Forward the line to the dialogue UI.
        /// </summary>
        /// <param name="line">The line to send to the dialogue views.</param>
        private void HandleLine(Line line)
        {
            // Get the localized line from our line provider
            CurrentLine = lineProvider.GetLocalizedLine(line);

            // Expand substitutions
            var text = Dialogue.ExpandSubstitutions(CurrentLine.RawText, CurrentLine.Substitutions);

            if (text == null)
            {
                Debug.LogWarning($"Dialogue Runner couldn't expand substitutions in Yarn Project [{ yarnProject.name }] node [{ CurrentNodeName }] with line ID [{ CurrentLine.TextID }]. "
                    + "This usually happens because it couldn't find text in the Localization. The line may not be tagged properly. "
                    + "Try re-importing this Yarn Program. "
                    + "For now, Dialogue Runner will swap in CurrentLine.RawText.");
                text = CurrentLine.RawText;
            }

            // Render the markup
            Dialogue.LanguageCode = lineProvider.LocaleCode;

            try
            {
                CurrentLine.Text = Dialogue.ParseMarkup(text);
            }
            catch (Markup.MarkupParseException e)
            {
                // Parsing the markup failed. We'll log a warning, and
                // produce a markup result that just contains the raw text.
                Debug.LogWarning($"Failed to parse markup in \"{text}\": {e.Message}");
                CurrentLine.Text = new Markup.MarkupParseResult
                {
                    Text = text,
                    Attributes = new List<Markup.MarkupAttribute>()
                };
            }

            // Clear the set of active dialogue views, just in case
            ActiveDialogueViews.Clear();

            // the following is broken up into two stages because otherwise if the 
            // first view happens to finish first once it calls dialogue complete
            // it will empty the set of active views resulting in the line being considered
            // finished by the runner despite there being a bunch of views still waiting
            // so we do it over two loops.
            // the first finds every active view and flags it as such
            // the second then goes through them all and gives them the line

            // Mark this dialogue view as active
            foreach (var dialogueView in dialogueViews)
            {
                if (dialogueView == null)
                {
                    continue;
                }

                ActiveDialogueViews.Add(dialogueView);
            }
            // Send line to all active dialogue views
            foreach (var dialogueView in dialogueViews)
            {
                if (dialogueView == null)
                {
                    continue;
                }

                dialogueView.RunLine(CurrentLine,
                    () => DialogueViewCompletedDelivery(dialogueView));
            }
        }

        // called by the runner when a view has signalled that it needs to interrupt the current line
        void InterruptLine()
        {
            ActiveDialogueViews.Clear();

            foreach (var dialogueView in dialogueViews)
            {
                if (dialogueView == null)
                {
                    continue;
                }

                ActiveDialogueViews.Add(dialogueView);
            }

            foreach (var dialogueView in dialogueViews)
            {
                dialogueView.InterruptLine(CurrentLine, () => DialogueViewCompletedInterrupt(dialogueView));
            }
        }

        /// <summary>
        /// Indicates to the DialogueRunner that the user has selected an
        /// option
        /// </summary>
        /// <param name="optionIndex">The index of the option that was
        /// selected.</param>
        /// <exception cref="InvalidOperationException">Thrown when the
        /// <see cref="IsOptionSelectionAllowed"/> field is <see
        /// langword="true"/>, which is the case when <see
        /// cref="DialogueViewBase.RunOptions(DialogueOption[],
        /// Action{int})"/> is in the middle of being called.</exception>
        void SelectedOption(int optionIndex)
        {
            if (IsOptionSelectionAllowed == false) {
                throw new InvalidOperationException("Selecting an option on the same frame that options are provided is not allowed. Wait at least one frame before selecting an option.");
            }
            
            // Mark that this is the currently selected option in the
            // Dialogue
            Dialogue.SetSelectedOption(optionIndex);

            if (runSelectedOptionAsLine)
            {
                foreach (var option in currentOptions.Options)
                {
                    if (option.ID == optionIndex)
                    {
                        HandleLine(option.Line);
                        return;
                    }
                }

                Debug.LogError($"Can't run selected option ({optionIndex}) as a line: couldn't find the option's associated {nameof(Line)} object");
                ContinueDialogue();
            }
            else
            {
                ContinueDialogue();
            }

        }

        /// <summary>
        /// Parses the command string inside <paramref name="command"/>,
        /// attempts to find a suitable handler from <see
        /// cref="commandHandlers"/>, and invokes it if found.
        /// </summary>
        /// <param name="command">The <see cref="Command"/> to run.</param>
        /// <param name="onSuccessfulDispatch">A method to run if a command
        /// was successfully dispatched to a game object. This method is
        /// not called if a registered command handler is not
        /// found.</param>
        /// <returns>True if the command was dispatched to a game object;
        /// false otherwise.</returns>
        CommandDispatchResult DispatchCommandToRegisteredHandlers(Command command, Action onSuccessfulDispatch)
        {
            return DispatchCommandToRegisteredHandlers(command.Text, onSuccessfulDispatch);
        }

        /// <inheritdoc cref="DispatchCommandToRegisteredHandlers(Command,
        /// Action)"/>
        /// <param name="command">The text of the command to
        /// dispatch.</param>
        public CommandDispatchResult DispatchCommandToRegisteredHandlers(string command, Action onSuccessfulDispatch)
        {
            var commandTokens = SplitCommandText(command).ToArray();

            if (commandTokens.Length == 0)
            {
                // Nothing to do.
                return CommandDispatchResult.NotFound;
            }

            var firstWord = commandTokens[0];

            if (commandHandlers.ContainsKey(firstWord) == false)
            {
                // We don't have a registered handler for this command, but
                // some other part of the game might.
                return CommandDispatchResult.NotFound;
            }

            var @delegate = commandHandlers[firstWord];
            var methodInfo = @delegate.Method;

            object[] finalParameters;

            try
            {
                finalParameters = ActionManager.ParseArgs(methodInfo, commandTokens);
            }
            catch (ArgumentException e)
            {
                Debug.LogError($"Can't run command {firstWord}: {e.Message}");
                return CommandDispatchResult.Failed;
            }

            if (typeof(YieldInstruction).IsAssignableFrom(methodInfo.ReturnType))
            {
                _currentRoutine = YarnRoutineWrapper.FromEnumerator(WaitForYieldInstruction(@delegate, finalParameters), onSuccessfulDispatch);
            }
            else if (typeof(void) == methodInfo.ReturnType)
            {
                // This method does not return anything. Invoke it and call
                // our completion handler.
                @delegate.DynamicInvoke(finalParameters);

                onSuccessfulDispatch();
            }
            else
            {
                Debug.LogError($"Cannot run command {firstWord}: the provided delegate does not return a valid type (permitted return types are YieldInstruction or void)");
                return CommandDispatchResult.Failed;
            }

            return CommandDispatchResult.Success;
        }
        
        private static IEnumerator WaitForYieldInstruction(Delegate @theDelegate, object[] finalParametersToUse)
        {
            object yieldInstruction = @theDelegate.DynamicInvoke(finalParametersToUse);
            yield return yieldInstruction;
        }

        public CommandDispatchResult DispatchCommandToGameObject(Command command, Action onSuccessfulDispatch)
        {
            // Call out to the string version of this method, because
            // Yarn.Command's constructor is only accessible from inside
            // Yarn Spinner, but we want to be able to unit test. So, we
            // extract it, and call the underlying implementation, which is
            // testable.
            return DispatchCommandToGameObject(command.Text, onSuccessfulDispatch);
        }
        
        public CommandDispatchResult DispatchCommandToGameObject(string command, Action onSuccessfulDispatch)
        {
            if (string.IsNullOrEmpty(command))
                throw new ArgumentException($"'{nameof(command)}' cannot be null or empty.", nameof(command));

            if (onSuccessfulDispatch is null)
                throw new ArgumentNullException(nameof(onSuccessfulDispatch));

            CommandDispatchResult commandExecutionResult = ActionManager.TryExecuteCommand(SplitCommandText(command).ToArray(), out object returnValue);
            if (commandExecutionResult != CommandDispatchResult.Success)
                return commandExecutionResult;

            if (returnValue is IEnumerator enumerator)
            {
                _currentRoutine = YarnRoutineWrapper.FromEnumerator(enumerator, onSuccessfulDispatch);
            }
            else
            {
                onSuccessfulDispatch();
            }
            
            return CommandDispatchResult.Success;
        }

        private void PrepareForLines(IEnumerable<string> lineIDs)
        {
            lineProvider.PrepareForLines(lineIDs);
        }

        /// <summary>
        /// Called when a <see cref="DialogueViewBase"/> has finished
        /// delivering its line.
        /// </summary>
        /// <param name="dialogueView">The view that finished delivering
        /// the line.</param>
        private void DialogueViewCompletedDelivery(DialogueViewBase dialogueView)
        {
            // A dialogue view just completed its delivery. Remove it from
            // the set of active views.
            ActiveDialogueViews.Remove(dialogueView);

            // Have all of the views completed? 
            if (ActiveDialogueViews.Count == 0)
            {
                DismissLineFromViews(dialogueViews);
            }
        }

        // this is similar to the above but for the interrupt
        // main difference is a line continues automatically every interrupt finishes
        private void DialogueViewCompletedInterrupt(DialogueViewBase dialogueView)
        {
            ActiveDialogueViews.Remove(dialogueView);

            if (ActiveDialogueViews.Count == 0)
            {
                DismissLineFromViews(dialogueViews);
            }
        }

        void ContinueDialogue()
        {
            CurrentLine = null;
            Dialogue.Continue();
        }

        /// <summary>
        /// Called by a <see cref="DialogueViewBase"/> derived class from
        /// <see cref="dialogueViews"/> to inform the <see
        /// cref="DialogueRunner"/> that the user intents to proceed to the
        /// next line.
        /// </summary>
        public void OnViewRequestedInterrupt()
        {
            if (_currentRoutine is { Running: true } && _currentRoutine.TryImmediateFinish())
                return;
            
            if (CurrentLine == null)
            {
                Debug.LogWarning("Dialogue runner was asked to advance but there is no current line");
                return;
            }

            // asked to advance when there are no active views
            // this means the views have already processed the lines as needed
            // so we can ignore this action
            if (ActiveDialogueViews.Count == 0)
            {
                Debug.Log("user requested advance, all views finished, ignoring interrupt");
                return;
            }

            // now because lines are fully responsible for advancement the only advancement allowed is interruption
            InterruptLine();
        }

        private void DismissLineFromViews(IEnumerable<DialogueViewBase> dialogueViews)
        {
            ActiveDialogueViews.Clear();

            foreach (var dialogueView in dialogueViews)
            {
                // Skip any dialogueView that is null or not enabled
                if (dialogueView == null)
                {
                    continue;
                }

                // we do this in two passes - first by adding each
                // dialogueView into ActiveDialogueViews, then by asking
                // them to dismiss the line - because calling
                // view.DismissLine might immediately call its completion
                // handler (which means that we'd be repeatedly returning
                // to zero active dialogue views, which means
                // DialogueViewCompletedDismissal will mark the line as
                // entirely done)
                ActiveDialogueViews.Add(dialogueView);
            }

            foreach (var dialogueView in dialogueViews)
            {
                if (dialogueView == null) 
                {
                    continue;
                }

                dialogueView.DismissLine(() => DialogueViewCompletedDismissal(dialogueView));
            }
        }

        private void DialogueViewCompletedDismissal(DialogueViewBase dialogueView)
        {
            // A dialogue view just completed dismissing its line. Remove
            // it from the set of active views.
            ActiveDialogueViews.Remove(dialogueView);

            // Have all of the views completed dismissal? 
            if (ActiveDialogueViews.Count == 0)
            {
                // Then we're ready to continue to the next piece of
                // content.
                ContinueDialogue();
            }
        }
#endregion

        /// <summary>
        /// Splits input into a number of non-empty sub-strings, separated
        /// by whitespace, and grouping double-quoted strings into a single
        /// sub-string.
        /// </summary>
        /// <param name="input">The string to split.</param>
        /// <returns>A collection of sub-strings.</returns>
        /// <remarks>
        /// This method behaves similarly to the <see
        /// cref="string.Split(char[], StringSplitOptions)"/> method with
        /// the <see cref="StringSplitOptions"/> parameter set to <see
        /// cref="StringSplitOptions.RemoveEmptyEntries"/>, with the
        /// following differences:
        ///
        /// <list type="bullet">
        /// <item>Text that appears inside a pair of double-quote
        /// characters will not be split.</item>
        ///
        /// <item>Text that appears after a double-quote character and
        /// before the end of the input will not be split (that is, an
        /// unterminated double-quoted string will be treated as though it
        /// had been terminated at the end of the input.)</item>
        ///
        /// <item>When inside a pair of double-quote characters, the string
        /// <c>\\</c> will be converted to <c>\</c>, and the string
        /// <c>\"</c> will be converted to <c>"</c>.</item>
        /// </list>
        /// </remarks>
        public static IEnumerable<string> SplitCommandText(string input)
        {
            var reader = new System.IO.StringReader(input.Normalize());

            int c;

            var results = new List<string>();
            var currentComponent = new System.Text.StringBuilder();

            while ((c = reader.Read()) != -1)
            {
                if (char.IsWhiteSpace((char)c))
                {
                    if (currentComponent.Length > 0)
                    {
                        // We've reached the end of a run of visible
                        // characters. Add this run to the result list and
                        // prepare for the next one.
                        results.Add(currentComponent.ToString());
                        currentComponent.Clear();
                    }
                    else
                    {
                        // We encountered a whitespace character, but
                        // didn't have any characters queued up. Skip this
                        // character.
                    }

                    continue;
                }
                else if (c == '\"')
                {
                    // We've entered a quoted string!
                    while (true)
                    {
                        c = reader.Read();
                        if (c == -1)
                        {
                            // Oops, we ended the input while parsing a
                            // quoted string! Dump our current word
                            // immediately and return.
                            results.Add(currentComponent.ToString());
                            return results;
                        }
                        else if (c == '\\')
                        {
                            // Possibly an escaped character!
                            var next = reader.Peek();
                            if (next == '\\' || next == '\"')
                            {
                                // It is! Skip the \ and use the character after it.
                                reader.Read();
                                currentComponent.Append((char)next);
                            }
                            else
                            {
                                // Oops, an invalid escape. Add the \ and
                                // whatever is after it.
                                currentComponent.Append((char)c);
                            }
                        }
                        else if (c == '\"')
                        {
                            // The end of a string!
                            break;
                        }
                        else
                        {
                            // Any other character. Add it to the buffer.
                            currentComponent.Append((char)c);
                        }
                    }

                    results.Add(currentComponent.ToString());
                    currentComponent.Clear();
                }
                else
                {
                    currentComponent.Append((char)c);
                }
            }

            if (currentComponent.Length > 0)
            {
                results.Add(currentComponent.ToString());
            }

            return results;
        }
        

        /// <summary>
        /// Loads all variables from the <see cref="PlayerPrefs"/> object into
        /// the Dialogue Runner's variable storage.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This method loads a string containing JSON from the <see
        /// cref="PlayerPrefs"/> object under the key <see cref="SaveKey"/>,
        /// deserializes that JSON, and then uses the resulting object to set
        /// all variables in <see cref="VariableStorage"/>.
        /// </para>
        /// <para>
        /// The loaded information can be stored via the <see
        /// cref="SaveStateToPlayerPrefs(string)"/> method.
        /// </para>
        /// </remarks>
        /// <param name="SaveKey">The key to use when storing the
        /// variables.</param>
        /// <returns><see langword="true"/> if the variables were successfully
        /// loaded from the player preferences; <see langword="false"/>
        /// otherwise.</returns>
        /// <seealso
        /// cref="VariableStorageBehaviour.SetAllVariables(Dictionary{string,
        /// float}, Dictionary{string, string}, Dictionary{string, bool},
        /// bool)"/>
        public bool LoadStateFromPlayerPrefs(string SaveKey = "YarnBasicSave")
        {
            if (PlayerPrefs.HasKey(SaveKey))
            {
                var saveData = PlayerPrefs.GetString(SaveKey);

                try
                {
                    var dictionaries = DeserializeAllVariablesFromJSON(saveData);
                    _variableStorage.SetAllVariables(dictionaries.Item1, dictionaries.Item2, dictionaries.Item3);

                    return true;
                }
                catch (ArgumentException e)
                {
                    Debug.LogWarning($"Unable to load saved data: {e.Message}");
                    return false;
                }
            }
            else
            {
                Debug.LogWarning("Attempted to load the runner previous state but found none saved");
                return false;
            }
        }

        /// <summary>
        /// Saves all variables in the Dialogue Runner's variable storage into
        /// the <see cref="PlayerPrefs"/> object.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This method serializes all variables in <see
        /// cref="VariableStorage"/> into a string containing JSON, and then
        /// stores that string in the <see cref="PlayerPrefs"/> object under the
        /// key <paramref name="SaveKey"/>.
        /// </para>
        /// <para>
        /// The stored information can be restored via the <see
        /// cref="LoadStateFromPlayerPrefs(string)"/> method.
        /// </para>
        /// </remarks>
        /// <param name="SaveKey">The key to use when storing the
        /// variables.</param>
        /// <seealso cref="VariableStorageBehaviour.GetAllVariables"/>
        public void SaveStateToPlayerPrefs(string SaveKey = "YarnBasicSave")
        {
            var data = SerializeAllVariablesToJSON();
            PlayerPrefs.SetString(SaveKey, data);
            PlayerPrefs.Save();
        }
        
        // takes in a JSON string and converts it into a tuple of dictionaries
        // intended to let you just dump these straight into the variable storage
        // throws exceptions if unable to convert or if the conversion half works
        private (Dictionary<string, float>, Dictionary<string, string>, Dictionary<string, bool>) DeserializeAllVariablesFromJSON(string jsonData)
        {
            SaveData data = JsonUtility.FromJson<SaveData>(jsonData);

            if (data.floatKeys == null && data.floatValues == null)
            {
                throw new ArgumentException("Provided JSON string was not able to extract numeric variables");
            }
            if (data.stringKeys == null && data.stringValues == null)
            {
                throw new ArgumentException("Provided JSON string was not able to extract string variables");
            }
            if (data.boolKeys == null && data.boolValues == null)
            {
                throw new ArgumentException("Provided JSON string was not able to extract boolean variables");
            }

            if (data.floatKeys.Length != data.floatValues.Length)
            {
                throw new ArgumentException("Number of keys and values of numeric variables does not match");
            }
            if (data.stringKeys.Length != data.stringValues.Length)
            {
                throw new ArgumentException("Number of keys and values of string variables does not match");
            }
            if (data.boolKeys.Length != data.boolValues.Length)
            {
                throw new ArgumentException("Number of keys and values of boolean variables does not match");
            }

            var floats = new Dictionary<string, float>();
            for (int i = 0; i < data.floatValues.Length; i++)
            {
                floats.Add(data.floatKeys[i], data.floatValues[i]);
            }
            var strings = new Dictionary<string, string>();
            for (int i = 0; i < data.stringValues.Length; i++)
            {
                strings.Add(data.stringKeys[i], data.stringValues[i]);
            }
            var bools = new Dictionary<string, bool>();
            for (int i = 0; i < data.boolValues.Length; i++)
            {
                bools.Add(data.boolKeys[i], data.boolValues[i]);
            }

            return (floats, strings, bools);
        }
        private string SerializeAllVariablesToJSON()
        {
            (var floats, var strings, var bools) = _variableStorage.GetAllVariables();

            SaveData data = new SaveData();
            data.floatKeys = floats.Keys.ToArray();
            data.floatValues = floats.Values.ToArray();
            data.stringKeys = strings.Keys.ToArray();
            data.stringValues = strings.Values.ToArray();
            data.boolKeys = bools.Keys.ToArray();
            data.boolValues = bools.Values.ToArray();

            return JsonUtility.ToJson(data, true);
        }

        [Serializable]
        private struct SaveData
        {
            public string[] floatKeys;
            public float[] floatValues;
            public string[] stringKeys;
            public string[] stringValues;
            public string[] boolKeys;
            public bool[] boolValues;
        }
    }
}