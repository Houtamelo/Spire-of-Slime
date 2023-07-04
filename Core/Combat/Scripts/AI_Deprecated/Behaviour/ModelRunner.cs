/*using System.Collections;
using Unity.Barracuda;
using UnityEngine;
using UnityEngine.Pool;
using Utility;

namespace Combat.AI.Behaviour
{
    public class ModelRunner : Singleton<ModelRunner>
    {
        public static Coroutine GetOutputAsync(in Model model, in float[] observations, in float[] actionMask, in Promise<ModelOutput> promise)
        {
            return Instance.StartCoroutine(GetOutputAsyncRoutine(model, observations, actionMask, promise));
        }
        
        private static IEnumerator GetOutputAsyncRoutine(Model model, float[] observations, float[] actionMask, Promise<ModelOutput> promise)
        {
            Debug.Assert(observations.Length == TensorConstants.ObservationsInputSize);
            Debug.Assert(actionMask.Length == TensorConstants.OutputSize);
            Debug.Assert(model != null);
            Debug.Assert(promise != null);
            
            using (IWorker worker = WorkerFactory.CreateWorker(WorkerFactory.Type.CSharpBurst, model))
            using (Tensor observationsTensor = new Tensor(1, TensorConstants.ObservationsInputSize, observations))
            using (Tensor actionMaskTensor = new Tensor(1, TensorConstants.OutputSize, actionMask))
            using (var _ = DictionaryPool<string, Tensor>.Get(out var inputs))
            {
                inputs.Add(TensorConstants.ObservationsName, observationsTensor);
                inputs.Add(TensorConstants.ActionMaskName, actionMaskTensor);

                Tensor output = worker.Execute(inputs).PeekOutput(TensorConstants.OutputName);

                yield return new WaitForCompletion(output);

                Option<ModelOutput> option = ModelOutput.FromTensor(output);
                promise.Resolve(option);

                foreach (Tensor input in inputs.Values) 
                    input.Dispose();
            }
        }
        
        public static Coroutine GetOutputAsync(in Model model, in float[] observations, in float[] actionMask, in Promise<DeterministicModelOutput> promise)
        {
            return Instance.StartCoroutine(GetOutputAsyncRoutine(model, observations, actionMask, promise));
        }
        
        private static IEnumerator GetOutputAsyncRoutine(Model model, float[] observations, float[] actionMask, Promise<DeterministicModelOutput> promise)
        {
            Debug.Assert(observations.Length == TensorConstants.ObservationsInputSize);
            Debug.Assert(actionMask.Length == TensorConstants.OutputSize);
            Debug.Assert(model != null);
            Debug.Assert(promise != null);
            
            using (IWorker worker = WorkerFactory.CreateWorker(WorkerFactory.Type.CSharpBurst, model))
            using (Tensor observationsTensor = new Tensor(1, TensorConstants.ObservationsInputSize, observations))
            using (Tensor actionMaskTensor = new Tensor(1, TensorConstants.OutputSize, actionMask))
            using (var _ = DictionaryPool<string, Tensor>.Get(out var inputs))
            {
                inputs.Add(TensorConstants.ObservationsName, observationsTensor);
                inputs.Add(TensorConstants.ActionMaskName, actionMaskTensor);

                Tensor output = worker.Execute(inputs).PeekOutput(TensorConstants.DeterministicOutputName);

                yield return new WaitForCompletion(output);

                Option<DeterministicModelOutput> option = DeterministicModelOutput.FromTensor(output);
                promise.Resolve(option);

                foreach (Tensor input in inputs.Values) 
                    input.Dispose();
            }
        }
    }
}*/