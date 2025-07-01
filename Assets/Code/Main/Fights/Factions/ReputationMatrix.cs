namespace Awaken.TG.Main.Fights.Factions {
    /// <summary>
    /// Runtime read-only representation of a faction's reputation matrix. Protected from editing. Easily accessible information about each reputation kind using 2d indexers.
    /// Based on Fallout New Vegas reputation system: https://fallout.fandom.com/wiki/Fallout:_New_Vegas_reputations 
    /// </summary>
    public readonly struct ReputationMatrix {
        ReputationRow[] ReputationTable { get; }
        
        public ReputationMatrix(ReputationRow[] reputationTable) {
            ReputationTable = reputationTable;
        }

        public ReputationInfo this[int index, int index2] => ReputationTable[index][index2];
    }
}