namespace Yarn {

    /// <summary>
    /// An exception that is thrown by <see cref="Dialogue"/> when there is an error in executing a <see cref="Program"/>.
    /// </summary>
    [System.Serializable]
    public class DialogueException : System.Exception
    {
        public DialogueException() { }
        public DialogueException(string message) : base(message) { }
        public DialogueException(string message, System.Exception inner) : base(message, inner) { }
        public DialogueException(
            System.Runtime.Serialization.SerializationInfo info,
            System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}

