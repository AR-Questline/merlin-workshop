#if UNITY_EDITOR
using System.Reflection;
using JetBrains.Annotations;
#endif

namespace Awaken.TG.Main.AI.Barks {
    public static class BarkBookmarks {
        public const string
            // Notice States
            NoticePlayerGeneric = nameof(NoticePlayerGeneric),
            SayGoodbyeGeneric = nameof(SayGoodbyeGeneric),

            NoticeWeaponDrawnGeneric = nameof(NoticeWeaponDrawnGeneric),
            NoticeItemDroppedGeneric = nameof(NoticeItemDroppedGeneric),
            NoticeHeroJumped = nameof(NoticeHeroJumped),
            NoticeHeroSneak = nameof(NoticeHeroSneak),
            NoticeWeaponUse = nameof(NoticeWeaponUse),

            // Alert States
            ToAlertDeadBodiesFound = nameof(ToAlertDeadBodiesFound),
            
            LookAtAlertSource = nameof(LookAtAlertSource),
            MoveToAlertSource = nameof(MoveToAlertSource),

            // Return to idle state
            ToIdleFromAlert = nameof(ToIdleFromAlert),
            ToIdleFromCombatDisengage = nameof(ToIdleFromCombatDisengage),
            ToIdleFromCombatVictory = nameof(ToIdleFromCombatVictory),

            // Generic Idle States
            Idle = nameof(Idle),
            WorkIdle = nameof(WorkIdle),
            SleepingIdle = nameof(SleepingIdle),

            // Responses
            BusyResponse = nameof(BusyResponse),
            SleepingResponse = nameof(SleepingResponse),

            // Physical Interactions
            BumpedInto = nameof(BumpedInto),
            SlidedInto = nameof(SlidedInto),

            // Combat States
            ToCombat = nameof(ToCombat),
            CombatIdle = nameof(CombatIdle),
            CombatOnHit = nameof(CombatOnHit),
            CombatOnGetHit = nameof(CombatOnGetHit),
            FleeingFromCombat = nameof(FleeingFromCombat),

            // Crimes
            CrimeOnTrespassing = nameof(CrimeOnTrespassing),
            CrimeToAlert = nameof(CrimeToAlert),
            CrimeToCombat = nameof(CrimeToCombat),
            CallGuards = nameof(CallGuards),
            
            // Other
            OpenShop = nameof(OpenShop),
            BountyHunterOnCriminalSight = nameof(BountyHunterOnCriminalSight);

#if UNITY_EDITOR
        [UsedImplicitly]
        public const string
            // Notice States
            NoticePlayerGenericDesc = "The NPC noticed the player and looked in their direction, e.g., 'Hey, where’s the rush?'",
            SayGoodbyeGenericDesc = "The player ended the conversation with the NPC (currently, there is mostly silence, but in most situations, the NPC could still say something after the dialogue).",

            NoticeWeaponDrawnGenericDesc = "The NPC noticed that the player drew a weapon. e.g., 'Hey, what are you doing with that weapon?'",
            NoticeItemDroppedGenericDesc = "The NPC noticed that the player dropped an item. e.g., 'You dropped something?'",
            NoticeHeroJumpedDesc = "The NPC noticed that the player is messing around and jumping. e.g., 'Hey, what are you doing?'",
            NoticeHeroSneakDesc = "The NPC noticed that the player is sneaking. e.g., 'I’ve got my eye on you!'",
            NoticeWeaponUseDesc = "The NPC noticed that the player is using a weapon. e.g., 'Watch out, you might hurt someone!'",

            // Alert States
            ToAlertDeadBodiesFoundDesc = "The NPC found a corpse, and enters an alert state. e.g., 'A body! Who could have done this?'",
            StartLookAtAlertSourceDesc = "Alert State: The NPC enters an alert state upon seeing something and looks in that direction — usually at the player. For example: 'Huh, what was that?' or 'Is someone there?'",
            StartWalkToAlertSourceDesc = "Alert State: The NPC starts walking towards something that caused alert. e.g., 'I need to check that out.",
            StartRunToAlertSourceDesc = "Alert State: The NPC starts running/jogging toward something that caused alert. e.g., 'I need to get check this out quickly!'",
            
            IdleLookAtAlertSourceDesc = "Alert State: The NPC is looking in the direction of something that caused alert and comments it. e.g., 'What’s going on over there?'",
            IdleWalkToAlertSourceDesc = "Alert State: The NPC is walking toward the source of the alert. e.g., 'I realy hope it’s nothing serious.'",
            IdleRunToAlertSourceDesc = "Alert State: The NPC runs/jogs toward the source of the alert. e.g., 'Whoever it is, they won’t get away!'",
            
            // Return to idle state
            ToIdleFromAlertDesc = "The NPC stopped looking for the player. e.g., 'He’s not here, must have run away.'",
            ToIdleFromCombatDisengageDesc = "The player fled from combat, and the NPC stopped chasing them. e.g., 'He ran away, no point in chasing him.'",
            ToIdleFromCombatVictoryDesc = "The NPC won the fight and returns to a normal state. e.g., 'I didn’t want this, but I had to defend myself.'",

            // Generic Idle States
            IdleDesc = "The NPC talks to themselves aimlessly. e.g., 'I'am so bored...'",
            WorkIdleDesc = "The NPC is working, commenting, or complaining about the job. e.g., 'I’ll get blisters from this shovel…'",
            SleepingIdleDesc = "The NPC is sleeping, snoring, or talking in their sleep.",
            
            // Responses
            BusyResponseDesc = "The player tries to talk to the NPC, but they are busy. e.g., 'Not now, I’m busy.'",
            SleepingResponseDesc = "The player tries to talk to the NPC, but they are sleeping. e.g., 'Leave me alone, I’m sleeping.'",

            // Physical Interactions
            BumpedIntoDesc = "The player walks into or pushes an NPC standing in their way. e.g., 'Hey, watch where you’re going!'",
            SlidedIntoDesc = "The player deliberately sprinted and slid into the NPC, causing damage and possibly knocking them over. e.g., 'Hey, what are you doing?'",
            
            // Combat State
            ToCombatDesc = "The NPC entered combat. e.g., 'So it has come to this… Defend yourself!'",
            CombatIdleDesc = "The NPC is in combat and shouts at their opponent or allies. e.g., 'I’ll get you soon!', 'Someone call the guards!'",
            CombatOnHitDesc = "The NPC hit their opponent in combat and comments on it. e.g., 'That’ll teach you!'",
            CombatOnGetHitDesc = "The NPC got hit by the player in combat and comments on it. e.g., 'Argh! You’ll pay for that!'",
            FleeingFromCombatDesc = "The NPC is running away from combat, e.g., 'Please help, this lunatic is trying to kill me!'",

            // Crimes
            CrimeOnTrespassingDesc = "The NPC saw the player in a restricted area. e.g., 'Hey, what are you doing here?'",
            CrimeToAlertDesc = "The NPC noticed an illegal action (e.g., an attempt to take someone’s apple) and warns the player. e.g., 'Stop that immediately!'",
            CrimeToCombatDesc = "The NPC saw an illegal action and decides to take matters into their own hands—either they already warned the player or the crime is too serious. e.g., 'That’s it, defend yourself!'",
            CallGuardsDesc = "The NPC saw an illegal action and runs away to call the guards — either they already warned the player, or the crime was severe. e.g., 'Guards! Help!'",
            
            // Other
            OpenShopDesc = "The player opened a shop interface with the NPC (the NPC says something in the background). The dialogue plays in the background while the trade screen is open. e.g., 'See anything you like?'",
            BountyHunterOnCriminalSightDesc = "A guard or similar NPC sees the player with a bounty and approaches them. e.g., 'Stop right there, Criminal Scum!'";
        
        public static string Editor_GetBookmarkDescription(string bookmark) {
            string descriptionFieldName = bookmark + "Desc";
            FieldInfo fieldInfo = typeof(BarkBookmarks).GetField(descriptionFieldName, BindingFlags.Public | BindingFlags.Static);

            if (fieldInfo != null && fieldInfo.FieldType == typeof(string)) {
                return (string)fieldInfo.GetValue(null);
            }

            return "Description not available.";
        }
#endif
    }
}