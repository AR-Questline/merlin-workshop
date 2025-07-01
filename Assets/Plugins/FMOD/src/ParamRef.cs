using System;

namespace FMODUnity
{
    [Serializable]
    public class ParamRef
    {
        public string Name;
        public float Value;
        public FMOD.Studio.PARAMETER_ID ID;
    }

    [Serializable]
    public struct ParamData {
        public FMOD.Studio.PARAMETER_ID ID;
        public float Value;
        public string Name;

        public bool HasValidID => ID.Equals(default) == false;
    }
}