using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace VectorSandboxLab.MemoryGame
{
    public sealed class BoardAreaLayoutWatcher : UIBehaviour
    {
        public event Action DimensionsChanged;

        protected override void OnRectTransformDimensionsChange()
        {
            base.OnRectTransformDimensionsChange();
            DimensionsChanged?.Invoke();
        }
    }
}
