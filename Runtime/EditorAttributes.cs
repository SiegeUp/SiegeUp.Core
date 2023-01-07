using System;
using UnityEngine;

namespace SiegeUp.Core
{
    public class OptionalAttribute : System.Attribute
    {
    }

    public class QuickEditAttribute : System.Attribute
    {
        [SerializeField]
        string visibleName;

        public QuickEditAttribute(string visibleName = null)
        {
            this.visibleName = visibleName;
        }

        public string VisibleName => visibleName;
    }

    [AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
    public class UniqueIdStringAttribute : PropertyAttribute
    {
    }
}