using System;

namespace UnityEngine.Localization.Pseudo
{
    /// <summary>
    /// Replaces characters with accented versions, to make it obvious if parts of the output are hard-coded in the program and can't be localized.
    /// </summary>
    [Serializable]
    public class Accenter : CharacterSubstitutor
    {
        /// <summary>
        /// Creates a new instance of the class.
        /// </summary>
        public Accenter()
        {
            Method = SubstitutionMethod.Map;
            AddDefaults();
        }

        /// <summary>
        /// Adds default character substitutions.
        /// </summary>
        public void AddDefaults()
        {
            // These mappings are based on the Google pseudo localization tool: https://code.google.com/archive/p/pseudolocalization-tool/
            ReplacementMap[' ']  = '\u2003';
            ReplacementMap['!']  = '\u00a1';
            ReplacementMap['"']  = '\u2033';
            ReplacementMap['#']  = '\u266f';
            ReplacementMap['$']  = '\u20ac';
            ReplacementMap['%']  = '\u2030';
            ReplacementMap['&']  = '\u214b';
            ReplacementMap['\''] = '\u00b4';
            ReplacementMap['*']  = '\u204e';
            ReplacementMap['+']  = '\u207a';
            ReplacementMap[']']  = '\u060c';
            ReplacementMap['-']  = '\u2010';
            ReplacementMap['.']  = '\u00b7';
            ReplacementMap['/']  = '\u2044';
            ReplacementMap['0']  = '\u24ea';
            ReplacementMap['1']  = '\u2460';
            ReplacementMap['2']  = '\u2461';
            ReplacementMap['3']  = '\u2462';
            ReplacementMap['4']  = '\u2463';
            ReplacementMap['5']  = '\u2464';
            ReplacementMap['6']  = '\u2465';
            ReplacementMap['7']  = '\u2466';
            ReplacementMap['8']  = '\u2467';
            ReplacementMap['9']  = '\u2468';
            ReplacementMap[':']  = '\u2236';
            ReplacementMap[';']  = '\u204f';
            ReplacementMap['<']  = '\u2264';
            ReplacementMap['=']  = '\u2242';
            ReplacementMap['>']  = '\u2265';
            ReplacementMap['?']  = '\u00bf';
            ReplacementMap['@']  = '\u055e';
            ReplacementMap['A']  = '\u00c5';
            ReplacementMap['B']  = '\u0181';
            ReplacementMap['C']  = '\u00c7';
            ReplacementMap['D']  = '\u00d0';
            ReplacementMap['E']  = '\u00c9';
            ReplacementMap['F']  = '\u0191';
            ReplacementMap['G']  = '\u011c';
            ReplacementMap['H']  = '\u0124';
            ReplacementMap['I']  = '\u00ce';
            ReplacementMap['J']  = '\u0134';
            ReplacementMap['K']  = '\u0136';
            ReplacementMap['L']  = '\u013b';
            ReplacementMap['M']  = '\u1e40';
            ReplacementMap['N']  = '\u00d1';
            ReplacementMap['O']  = '\u00d6';
            ReplacementMap['P']  = '\u00de';
            ReplacementMap['Q']  = '\u01ea';
            ReplacementMap['R']  = '\u0154';
            ReplacementMap['S']  = '\u0160';
            ReplacementMap['T']  = '\u0162';
            ReplacementMap['U']  = '\u00db';
            ReplacementMap['V']  = '\u1e7c';
            ReplacementMap['W']  = '\u0174';
            ReplacementMap['X']  = '\u1e8a';
            ReplacementMap['Y']  = '\u00dd';
            ReplacementMap['Z']  = '\u017d';
            ReplacementMap['[']  = '\u2045';
            ReplacementMap['\\'] = '\u2216';
            ReplacementMap[']']  = '\u2046';
            ReplacementMap['^']  = '\u02c4';
            ReplacementMap['_']  = '\u203f';
            ReplacementMap['`']  = '\u2035';
            ReplacementMap['a']  = '\u00e5';
            ReplacementMap['b']  = '\u0180';
            ReplacementMap['c']  = '\u00e7';
            ReplacementMap['d']  = '\u00f0';
            ReplacementMap['e']  = '\u00e9';
            ReplacementMap['f']  = '\u0192';
            ReplacementMap['g']  = '\u011d';
            ReplacementMap['h']  = '\u0125';
            ReplacementMap['i']  = '\u00ee';
            ReplacementMap['j']  = '\u0135';
            ReplacementMap['k']  = '\u0137';
            ReplacementMap['l']  = '\u013c';
            ReplacementMap['m']  = '\u0271';
            ReplacementMap['n']  = '\u00f1';
            ReplacementMap['o']  = '\u00f6';
            ReplacementMap['p']  = '\u00fe';
            ReplacementMap['q']  = '\u01eb';
            ReplacementMap['r']  = '\u0155';
            ReplacementMap['s']  = '\u0161';
            ReplacementMap['t']  = '\u0163';
            ReplacementMap['u']  = '\u00fb';
            ReplacementMap['v']  = '\u1e7d';
            ReplacementMap['w']  = '\u0175';
            ReplacementMap['x']  = '\u1e8b';
            ReplacementMap['y']  = '\u00fd';
            ReplacementMap['z']  = '\u017e';
            ReplacementMap['|']  = '\u00a6';
            ReplacementMap['~']  = '\u02de';
        }
    }
}
