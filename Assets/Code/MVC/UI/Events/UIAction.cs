using System.Collections.Generic;
using Awaken.TG.Main.Utility;
using Awaken.TG.Main.Utility.UI;
using Awaken.TG.MVC.UI.Handlers.Focuses;
using Rewired;
using UnityEngine;

namespace Awaken.TG.MVC.UI.Events {
    public class UIAction : UIEvent {
        public InputActionData Data { get; set; }

        public Player Player => Data.player;
        public string Name => Data.actionName;
        public int Id => Data.actionId;
        [UnityEngine.Scripting.Preserve] public bool IsButtonDown => false;//Player.GetButtonDown(Id);
        [UnityEngine.Scripting.Preserve] public bool IsButtonUp => false;//Player.GetButtonUp(Id);
        [UnityEngine.Scripting.Preserve] public bool IsButtonHeld => false;//Player.GetButton(Id);
        [UnityEngine.Scripting.Preserve] public bool IsButtonLongHeld => false;//Player.GetButtonLongPress(Id);
        [UnityEngine.Scripting.Preserve] public bool IsButtonLongPressUp => false;//Player.GetButtonLongPressUp(Id);
        public float Value => 0;//Player.GetAxis(Id);
        public float ValueRaw => 0;//Player.GetAxisRaw(Id);
        public bool IsOutsideOfDeadZone => Mathf.Abs(ValueRaw) > RewiredHelper.DeadZone;

        public static UIAction CreateButton(GameUI gameUI, UIPosition position, Player player, InputAction action, InputActionData data = new InputActionData()) {
            // if (data.player == null) {
            //     data = CreateData(player, action);
            // }

            // int id = action.id;
            //
            // if (player.GetButtonDown(id)) {
            //     if (data.actionName == KeyBindings.UI.Generic.Accept) {
            //         // gamepad submit button
            //         return new UISubmitAction {GameUI = gameUI, Position = position, Data = data};
            //     } else if (data.actionName == KeyBindings.UI.Generic.Cancel) {
            //         return new UICancelAction {GameUI = gameUI, Position = position, Data = data};
            //     } else {
            //         return new UIKeyDownAction {GameUI = gameUI, Position = position, Data = data};
            //     }
            // } else if (player.GetButtonUp(id)) {
            //     if (player.GetButtonLongPressUp(id)) {
            //         return new UIKeyLongUpAction {GameUI = gameUI, Position = position, Data = data};
            //     }
            //     return new UIKeyUpAction {GameUI = gameUI, Position = position, Data = data};
            // } else if (player.GetButton(id)) {
            //     if (player.GetButtonLongPress(id)) {
            //         return new UIKeyLongHeldAction {GameUI = gameUI, Position = position, Data = data};
            //     }
            //     return new UIKeyHeldAction {GameUI = gameUI, Position = position, Data = data};
            // } else {
            //     return null;
            // }
            return null;
        }

        public static InputActionData CreateData(Player player, InputAction action) {
            InputActionData data = new InputActionData {
                player = player,
                actionName = action.name,
                actionId = action.id,
            };
            return data;
        }
        
        public static UIAction CreateNaviAction(GameUI gameUI, UIPosition position, Player player, InputActionData actionData)
        {
            float horizontal = 0;//player.GetAxisRaw("Horizontal");
            float vertical = 0;//player.GetAxisRaw("Vertical");
            
            NaviDirection direction = null;
            if (horizontal > RewiredHelper.NaviThreshold && NaviDirection.Right.Use()) {
                direction = NaviDirection.Right;
            } else if (horizontal < -RewiredHelper.NaviThreshold && NaviDirection.Left.Use()) {
                direction = NaviDirection.Left;
            } else if (vertical > RewiredHelper.NaviThreshold && NaviDirection.Up.Use()) {
                direction = NaviDirection.Up;
            } else if (vertical < -RewiredHelper.NaviThreshold && NaviDirection.Down.Use()) {
                direction = NaviDirection.Down;
            }

            return direction != null ? new UINaviAction {direction = direction, GameUI = gameUI, Position = position, Data = actionData} : null;
        }
    }

    public class UIKeyAction : UIAction { }
    public class UIKeyDownAction : UIKeyAction { }
    public class UIKeyUpAction : UIKeyAction { }
    public class UIKeyLongUpAction : UIKeyUpAction { }
    public class UIKeyHeldAction : UIKeyAction { }
    public class UIKeyLongHeldAction : UIKeyHeldAction { }
    public class UISubmitAction : UIKeyDownAction, ISubmit { }
    public class UICancelAction : UIKeyDownAction { }

    public class UINaviAction : UIAction {
        public NaviDirection direction;
    }
    
    public class UIAxisAction : UIAction { }

    public struct InputActionData {
        public Player player;
        public string actionName;
        public int actionId;
    }
}