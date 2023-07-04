/*using System;
using System.Buffers;
using NetFabric.Hyperlinq;
using Unity.Barracuda;
using UnityEngine;
using Random = System.Random;

namespace Combat.AI.Behaviour
{
    public class ModelOutput : IDisposable
    {
        public float this[int index] => _disposed ? 0 : _lease.Rented[index];

        private Lease<float> _lease;
        
        private ModelOutput(Lease<float> lease)
        {
            _lease = lease;
        }

        private bool _disposed;
        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            _lease.Dispose();
            _lease = default;
        }
        
        public OutPutEnumerator GetEnumerator() => new(this);

        public int GetRandomWeightedIndex(Random random)
        {
            float total = 0f;
            foreach (float weight in this) 
                total += weight;

            double randomPoint = random.NextDouble() * total;

            int i = 0;
            foreach(float weight in this)
            {
                if (randomPoint < weight)
                    return i;
                
                randomPoint -= weight;
                i++;
            }
            
            return TensorConstants.OutputSize - 1;
        }
        
        public static Utility.Option<ModelOutput> FromTensor(Tensor output)
        {
            if (output == null)
                return Utility.Option<ModelOutput>.None;

            Lease<float> lease = ArrayPool<float>.Shared.Lease(TensorConstants.OutputSize);
            for (int i = 0; i < TensorConstants.OutputSize; i++) 
                lease.Rented[i] = Mathf.Exp(output[0, i]);

            float total = 0;
            foreach (float prob in lease) 
                total += prob;

            for (int i = 0; i < TensorConstants.OutputSize; i++) 
                lease.Rented[i] /= total;

            return Utility.Option<ModelOutput>.Some(new ModelOutput(lease));
        }

        public struct OutPutEnumerator
        {
            public float Current { get; set; }
            private readonly ModelOutput _modelOutput;
            private int _currentIndex;

            public OutPutEnumerator(ModelOutput modelOutput)
            {
                _modelOutput = modelOutput;
                Current = default;
                _currentIndex = 0;
            }

            public bool MoveNext()
            {
                _currentIndex++;
                if (_currentIndex < TensorConstants.OutputSize)
                    Current = _modelOutput[_currentIndex];

                return false;
            }

            public void Reset()
            {
                _currentIndex = 0;
            }

            public OutPutEnumerator GetEnumerator() => this;
        }
    }
}*/