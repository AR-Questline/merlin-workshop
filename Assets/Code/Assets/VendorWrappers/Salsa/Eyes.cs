using System.Collections.Generic;
using CrazyMinnow.SALSA;
using UnityEngine;

namespace Awaken.VendorWrappers.Salsa {
    public class Eyes : MonoBehaviour {
        public bool inspStateFoldoutReferences;
        public bool inspStateFoldoutHeads;
        public bool inspStateFoldoutHeadProps = true;
        public bool inspStateFoldoutAnimBones;
        public bool inspStateFoldoutEyes;
        public bool inspStateFoldoutEyeProps = true;
        public bool inspStateFoldoutEyelids;
        public bool inspStateFoldoutEyelidBlinkProps = true;
        public bool inspStateFoldoutEyelidTrackProps = true;
        public bool drawEyeBoneCaptureControls;
        public bool drawBlinkBoneCaptureControls;
        public bool drawTrackBoneCaputreControls;
        public bool headIsRoot;
        public bool configReadyHead = true;
        public bool configReadyEye = true;
        public bool configReadyBlink = true;
        public bool configReadyTrack = true;
     
        public Transform characterRoot;
        public QueueProcessor queueProcessor;
        public Transform lookTarget;
        public bool useAffinity;
        public float affinityPercentage = 0.75f;
        public bool hasAffinity = true;
        private bool prevHasAffinity = true;
        public Vector2 affinityTimerRange = new Vector2(2f, 5f);
        private float affinityTimer;
        public float updateDelay = 0.07f;
        private float updateTimer;
        public bool useSpriteFlipping;
        public FlipType flipType;
        public Transform flipScale;
        public SpriteRenderer flipSprite;
        public bool parentRandTargets;
        public bool showSceneGizmos = true;
        private bool configNotReadyNotified;
        public bool warnOnNullRefs = true;
        public bool flipState;
        private bool prevHeadFlipState;
        public List<AnimCancel> animCancel = new List<AnimCancel>();
        private ProcDirection procDir = new ProcDirection();
        public const float ANIMATION_DURATION_ON = -1f;
        public bool headEnabled = true;
        public Eyes.HeadTemplates headTemplate;
        public bool head2DProcessing;
        public Vector3 headTargetOffset = Vector3.zero;
        public Vector3 headClamp = new Vector3(70f, 140f, 45f);
        public bool headFacingCamera;
        public bool headRandom = true;
        public Vector3 headRandomFov = new Vector3(5f, 45f, 22.5f);
        public Vector3 headRandFovHalf;
        public Vector2 headRandTimerRange = new Vector2(5f, 10f);
        public float headRandTimer;
        public bool headUseDistExtents;
        public Vector2 headRandDistRange = new Vector2(1f, 1f);
        public bool updateHeadFwdCenterRefs = true;
        public float headTargetRadius = 0.01f;
        private List<ProcVecs> trackHeadDirs2D = new List<ProcVecs>();
        public List<ProcTrans> headBones = new List<ProcTrans>();
        public List<ProcTransOffset> headRefsFwd = new List<ProcTransOffset>();
        public List<ProcTrans> headRefsFix = new List<ProcTrans>();
        public Transform headRefCenter;
        public List<ProcDirections> newHeadDirs = new List<ProcDirections>();
        public Transform headTarget;
        public Vector3 headTarget2D;
        public List<EyesExpression> heads = new List<EyesExpression>();
        public float headTarget2DDot;
        public bool eyeEnabled = true;
        public Eyes.EyeTemplates eyeTemplate;
        public Eyes.SectorCount sectorCount = Eyes.SectorCount.Nine;
        public Eyes.SectorCount prevSectorCount;
        private ExpressionComponent.DirectionType curEyeSector;
        private ExpressionComponent.DirectionType prevEyeSector;
        private ExpressionComponent.DirectionType lastSector;
        public ExpressionComponent.ControlType eyeControlType;
        public float forwardSectorRadius = 1f;
        private float forwardSectCurDist;
        public bool eye2DProcessing;
        public float eye2DDepth = 1f;
        public float eyePosPercentX;
        public float eyePosPercentY;
        public bool useEyeShapes;
        public bool useEyeSectors;
        public Vector3 eyeClamp = new Vector3(25f, 45f, 0.0f);
        public bool eyeRandom = true;
        public Vector3 eyeRandomFov = new Vector3(2.5f, 5f, 10f);
        private Vector3 eyeRandFovHalf;
        public Vector2 eyeRandTrackFov = new Vector2(0.1f, 0.05f);
        public Vector2 eyeRandTrackFovAffinity = new Vector2(0.2f, 0.1f);
        public Vector2 eyeRandTimerRange = new Vector2(0.25f, 1f);
        public float eyeRandTimer;
        public bool eyeUseDistExtents;
        public Vector2 eyeRandDistRange = new Vector2(1f, 1f);
        public float eyeTargetRadius = 0.01f;
        private Vector3 trackEyeDir2D;
        private float trackEyeDeg2D;
        public List<ProcTrans> eyeBones = new List<ProcTrans>();
        public List<ProcTransOffset> eyeRefsFwd = new List<ProcTransOffset>();
        public List<ProcTrans> eyeRefsFix = new List<ProcTrans>();
        public Transform eyeRefCenter;
        public List<ProcDirections> newEyeDirs = new List<ProcDirections>();
        public Transform eyeTarget;
        public Vector3 eyeTarget2D;
        public List<EyesExpression> eyes = new List<EyesExpression>();
        private Vector3 eyePosY;
        private Vector3 eyePosX;
        private List<ProcLLPairs> eyePosLLPairs = new List<ProcLLPairs>();
        public int eyeExpIndex;
        public string[] eyeExpNames;
        public Renderer eyeExpRend;
        public int eyeExpRendItemCount = 2;
        public bool eyeExpRendIsNullOrSmr = true;
        public bool blinkEnabled = true;
        public Eyes.EyelidTemplates eyelidTemplate;
        public Eyes.EyelidSelection eyelidSelection;
        public ExpressionComponent.ControlType eyelidControlType;
        public bool eyelid2DProcessing;
        public float blinkOn;
        public float blinkHold;
        public float blinkOff;
        public bool blinkRandom = true;
        public Vector2 blinkRandTimerRange = new Vector2(0.5f, 5f);
        public float blinkRandTimer;
        [Range(0.0f, 1f)]
        public float eyelidPercentEyes = 0.5f;
        public float trackPercent;
        public List<EyesExpression> blinklids = new List<EyesExpression>();
        public int eyelidExpIndex;
        public string[] eyelidExpNames;
        public Renderer eyelidExpRend;
        public int eyelidExpRendItemCount = 1;
        public bool eyelidExpRendIsNullOrSmr = true;
        private bool isBlinking;
        private float isBlinkingTimer;
        private float isBlinkingDuration;
        public bool trackEnabled = true;
        public List<ProcTrans> trackBones = new List<ProcTrans>();
        public List<EyesExpression> tracklids = new List<EyesExpression>();
        private TformBase eyelidTrackTForm;
        private float uepAmountSign;
        
        
        public enum FlipType
        {
            Transform_X,
            Transform_Y,
            SpriteRenderer_X,
            SpriteRenderer_Y,
        }

        public enum ClampAxes
        {
            axesXY,
            axisZ,
        }

        public enum HeadTemplates
        {
            None,
            Bone_Rotation_XY,
            Bone_Rotation_Z,
        }

        public enum EyeTemplates
        {
            None,
            Bone_Rotation,
            Bone_Position,
            BlendShapes,
            Sprite_Sectors,
            Material_Sectors,
            Texture_Sectors,
        }

        public enum SectorCount
        {
            Two,
            Three,
            Five,
            Nine,
        }

        public enum EyelidTemplates
        {
            None,
            Bone_Rotation,
            Bone_Position,
            BlendShapes,
            Sprite_Swap,
            Material_Swap,
            Texture_Swap,
            UMA,
        }

        public enum EyelidSelection
        {
            Both,
            Upper,
            Lower,
        }
    }
}