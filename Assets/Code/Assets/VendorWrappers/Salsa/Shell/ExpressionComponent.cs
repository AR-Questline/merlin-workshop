using UnityEngine;

namespace CrazyMinnow.SALSA
{
    public class ExpressionComponent
    {
        public string name;
        [SerializeField]
        public IExpressionController controller;
        public ControlType controlType;
        public LipsyncControlType lipsyncControlType;
        public EmoteControlType emoteControlType;
        public EyesControlType eyesControlType = EyesControlType.Bone;
        public float durationDelay;
        public float durationOn;
        public float durationHold;
        public float durationOff;
        public bool isSmoothDisable;
        public bool isPersistent;
        public ExpressionType expressionType;
        public LerpEasings.EasingType easing;
        public ExpressionHandler expressionHandler;
        public bool isAnimatorControlled;
        public bool useOffset;
        public bool useOffsetFollow = true;
        public bool inspFoldout = true;
        public bool enabled = true;
        public bool isBonePreviewUpdated;
        public float frac = 1f;
        public DirectionType directionType;
        
        public enum DirectionType
        {
            None,
            UpperLeft,
            Upper,
            UpperRight,
            Left,
            Center,
            Right,
            LowerLeft,
            Lower,
            LowerRight,
        }

        public enum ExpressionType
        {
            Lipsync,
            Emote,
            Head,
            Eye,
            Eyelid,
            Blink,
        }

        public enum ControlType
        {
            Shape,
            Bone,
            Sprite,
            UguiSprite,
            Texture,
            Material,
            UMA,
            Animator,
            Event,
        }

        public enum LipsyncControlType
        {
            Shape,
            Bone,
            Sprite,
            UguiSprite,
            Texture,
            Material,
            UMA,
            Animator,
            Event,
        }

        public enum EmoteControlType
        {
            Shape,
            Bone,
            Sprite,
            UguiSprite,
            Texture,
            Material,
            UMA,
            Animator,
            Event,
        }

        public enum EyesControlType
        {
            Shape,
            Bone,
            Sprite,
            Texture,
            Material,
            UMA,
        }

        public enum ExpressionHandler
        {
            OneWay,
            RoundTrip,
        }
    }
}