using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Nguyen.Event
{
    public abstract class GenericEventChannelSO<T> : ScriptableObject
    {
        [Tooltip("The action to perform; Listeners subscribe to this UnityAction")]
        public UnityAction<T> OnEventRaised;
 
        public void RaiseEvent(T parameter)
        {

            if (OnEventRaised == null)
                return;

            OnEventRaised.Invoke(parameter);

        }
    }

}
