using System;

namespace Awaken.Utility.Maths {
    public static class NormalDistribution {
        /// <summary>
        /// Computes the cumulative distribution (CDF) of the distribution at x, i.e. P(X ≤ x).
        /// </summary>
        /// <param name="x">The location at which to compute the cumulative distribution function.</param>
        /// <param name="mean">The mean (μ) of the normal distribution.</param>
        /// <param name="stddev">The standard deviation (σ) of the normal distribution. Range: σ ≥ 0.</param>
        /// <returns>the cumulative distribution at location <paramref name="x" />.</returns>
        /// <seealso cref="M:MathNet.Numerics.Distributions.Normal.CumulativeDistribution(System.Double)" />
        /// <remarks>MATLAB: normcdf</remarks>
        public static double CDF(double mean, double stddev, double x) {
            if (stddev < 0.0)
                throw new ArgumentException("Invalid parametrization for the distribution.");
            return 0.5 * Erfc((mean - x) / (stddev * 1.4142135623730951));
        }

        /// <summary>
        /// Computes the inverse of the cumulative distribution function (InvCDF) for the distribution
        /// at the given probability. This is also known as the quantile or percent point function.
        /// </summary>
        /// <param name="p">The location at which to compute the inverse cumulative density.</param>
        /// <param name="mean">The mean (μ) of the normal distribution.</param>
        /// <param name="stddev">The standard deviation (σ) of the normal distribution. Range: σ ≥ 0.</param>
        /// <returns>the inverse cumulative density at <paramref name="p" />.</returns>
        /// <seealso cref="M:MathNet.Numerics.Distributions.Normal.InverseCumulativeDistribution(System.Double)" />
        /// <remarks>MATLAB: norminv</remarks>
        public static double InvCDF(double mean, double stddev, double p) {
            if (stddev < 0.0)
                throw new ArgumentException("Invalid parametrization for the distribution.");
            return mean - stddev * 1.4142135623730951 * ErfcInv(2.0 * p);
        }

        /// <summary>Calculates the complementary error function.</summary>
        /// <param name="x">The value to evaluate.</param>
        /// <returns>the complementary error function evaluated at given value.</returns>
        /// <remarks>
        ///     <list type="bullet">
        ///         <item>returns 0 if <c>x == double.PositiveInfinity</c>.</item>
        ///         <item>returns 2 if <c>x == double.NegativeInfinity</c>.</item>
        ///     </list>
        /// </remarks>
        public static double Erfc(double x) {
            if (x == 0.0)
                return 1.0;
            if (double.IsPositiveInfinity(d: x))
                return 0.0;
            if (double.IsNegativeInfinity(d: x))
                return 2.0;
            return double.IsNaN(d: x) ? double.NaN : ErfImp(z: x, true);
        }

        /// <summary>Implementation of the error function.</summary>
        /// <param name="z">Where to evaluate the error function.</param>
        /// <param name="invert">Whether to compute 1 - the error function.</param>
        /// <returns>the error function.</returns>
        static double ErfImp(double z, bool invert) {
            if (z < 0.0) {
                if (!invert)
                    return -ErfImp(z: -z, false);
                return z < -0.5 ? 2.0 - ErfImp(z: -z, true) : 1.0 + ErfImp(z: -z, false);
            }

            double num1;
            if (z < 0.5) {
                num1 = z >= 1E-10
                    ? z * 1.125 + z * PolynomialEvaluate(z: z, coefficients: ErfImpAn) / PolynomialEvaluate(z: z, coefficients: ErfImpAd)
                    : z * 1.125 + z * 0.0033791670955125737;
            } else if (z < 110.0) {
                invert = !invert;
                double num2;
                double num3;
                if (z < 0.75) {
                    num2 = PolynomialEvaluate(z - 0.5, coefficients: ErfImpBn) / PolynomialEvaluate(z - 0.5, coefficients: ErfImpBd);
                    num3 = 0.3440242111682892;
                } else if (z < 1.25) {
                    num2 = PolynomialEvaluate(z - 0.75, coefficients: ErfImpCn) / PolynomialEvaluate(z - 0.75, coefficients: ErfImpCd);
                    num3 = 0.4199909269809723;
                } else if (z < 2.25) {
                    num2 = PolynomialEvaluate(z - 1.25, coefficients: ErfImpDn) / PolynomialEvaluate(z - 1.25, coefficients: ErfImpDd);
                    num3 = 0.48986250162124634;
                } else if (z < 3.5) {
                    num2 = PolynomialEvaluate(z - 2.25, coefficients: ErfImpEn) / PolynomialEvaluate(z - 2.25, coefficients: ErfImpEd);
                    num3 = 0.5317370891571045;
                } else if (z < 5.25) {
                    num2 = PolynomialEvaluate(z - 3.5, coefficients: ErfImpFn) / PolynomialEvaluate(z - 3.5, coefficients: ErfImpFd);
                    num3 = 0.5489973425865173;
                } else if (z < 8.0) {
                    num2 = PolynomialEvaluate(z - 5.25, coefficients: ErfImpGn) / PolynomialEvaluate(z - 5.25, coefficients: ErfImpGd);
                    num3 = 0.5571740865707397;
                } else if (z < 11.5) {
                    num2 = PolynomialEvaluate(z - 8.0, coefficients: ErfImpHn) / PolynomialEvaluate(z - 8.0, coefficients: ErfImpHd);
                    num3 = 0.5609807968139648;
                } else if (z < 17.0) {
                    num2 = PolynomialEvaluate(z - 11.5, coefficients: ErfImpIn) / PolynomialEvaluate(z - 11.5, coefficients: ErfImpId);
                    num3 = 0.5626493692398071;
                } else if (z < 24.0) {
                    num2 = PolynomialEvaluate(z - 17.0, coefficients: ErfImpJn) / PolynomialEvaluate(z - 17.0, coefficients: ErfImpJd);
                    num3 = 0.5634598135948181;
                } else if (z < 38.0) {
                    num2 = PolynomialEvaluate(z - 24.0, coefficients: ErfImpKn) / PolynomialEvaluate(z - 24.0, coefficients: ErfImpKd);
                    num3 = 0.5638477802276611;
                } else if (z < 60.0) {
                    num2 = PolynomialEvaluate(z - 38.0, coefficients: ErfImpLn) / PolynomialEvaluate(z - 38.0, coefficients: ErfImpLd);
                    num3 = 0.5640528202056885;
                } else if (z < 85.0) {
                    num2 = PolynomialEvaluate(z - 60.0, coefficients: ErfImpMn) / PolynomialEvaluate(z - 60.0, coefficients: ErfImpMd);
                    num3 = 0.5641309022903442;
                } else {
                    num2 = PolynomialEvaluate(z - 85.0, coefficients: ErfImpNn) / PolynomialEvaluate(z - 85.0, coefficients: ErfImpNd);
                    num3 = 0.5641584396362305;
                }

                double num4 = Math.Exp(-z * z) / z;
                num1 = num4 * num3 + num4 * num2;
            } else {
                num1 = 0.0;
                invert = !invert;
            }

            if (invert)
                num1 = 1.0 - num1;
            return num1;
        }

        /// <summary>Calculates the complementary inverse error function evaluated at z.</summary>
        /// <returns>The complementary inverse error function evaluated at given value.</returns>
        /// <remarks> We have tested this implementation against the arbitrary precision mpmath library
        /// and found cases where we can only guarantee 9 significant figures correct.
        ///     <list type="bullet">
        ///         <item>returns double.PositiveInfinity if <c>z &lt;= 0.0</c>.</item>
        ///         <item>returns double.NegativeInfinity if <c>z &gt;= 2.0</c>.</item>
        ///     </list>
        /// </remarks>
        /// <summary>calculates the complementary inverse error function evaluated at z.</summary>
        /// <param name="z">value to evaluate.</param>
        /// <returns>the complementary inverse error function evaluated at Z.</returns>
        public static double ErfcInv(double z) {
            if (z <= 0.0)
                return double.PositiveInfinity;
            if (z >= 2.0)
                return double.NegativeInfinity;
            double q;
            double p;
            double s;
            if (z > 1.0) {
                q = 2.0 - z;
                p = 1.0 - q;
                s = -1.0;
            } else {
                p = 1.0 - z;
                q = z;
                s = 1.0;
            }

            return ErfInvImpl(p: p, q: q, s: s);
        }

        /// <summary>The implementation of the inverse error function.</summary>
        /// <param name="p">First intermediate parameter.</param>
        /// <param name="q">Second intermediate parameter.</param>
        /// <param name="s">Third intermediate parameter.</param>
        /// <returns>the inverse error function.</returns>
        static double ErfInvImpl(double p, double q, double s) {
            double num1;
            if (p <= 0.5) {
                double num2 = p * (p + 10.0);
                double num3 = PolynomialEvaluate(z: p, coefficients: ErvInvImpAn) / PolynomialEvaluate(z: p, coefficients: ErvInvImpAd);
                num1 = num2 * 0.08913147449493408 + num2 * num3;
            } else if (q >= 0.25) {
                double num4 = Math.Sqrt(-2.0 * Math.Log(d: q));
                double z = q - 0.25;
                double num5 = 2.249481201171875 + PolynomialEvaluate(z: z, coefficients: ErvInvImpBn) / PolynomialEvaluate(z: z, coefficients: ErvInvImpBd);
                num1 = num4 / num5;
            } else {
                double num6 = Math.Sqrt(-Math.Log(d: q));
                if (num6 < 3.0) {
                    double z = num6 - 1.125;
                    double num7 = PolynomialEvaluate(z: z, coefficients: ErvInvImpCn) / PolynomialEvaluate(z: z, coefficients: ErvInvImpCd);
                    num1 = 0.807220458984375 * num6 + num7 * num6;
                } else if (num6 < 6.0) {
                    double z = num6 - 3.0;
                    double num8 = PolynomialEvaluate(z: z, coefficients: ErvInvImpDn) / PolynomialEvaluate(z: z, coefficients: ErvInvImpDd);
                    num1 = 0.9399557113647461 * num6 + num8 * num6;
                } else if (num6 < 18.0) {
                    double z = num6 - 6.0;
                    double num9 = PolynomialEvaluate(z: z, coefficients: ErvInvImpEn) / PolynomialEvaluate(z: z, coefficients: ErvInvImpEd);
                    num1 = 0.9836282730102539 * num6 + num9 * num6;
                } else if (num6 < 44.0) {
                    double z = num6 - 18.0;
                    double num10 = PolynomialEvaluate(z: z, coefficients: ErvInvImpFn) / PolynomialEvaluate(z: z, coefficients: ErvInvImpFd);
                    num1 = 0.9971456527709961 * num6 + num10 * num6;
                } else {
                    double z = num6 - 44.0;
                    double num11 = PolynomialEvaluate(z: z, coefficients: ErvInvImpGn) / PolynomialEvaluate(z: z, coefficients: ErvInvImpGd);
                    num1 = 0.9994134902954102 * num6 + num11 * num6;
                }
            }

            return s * num1;
        }

        /// <summary>
        /// Evaluate a polynomial at point x.
        /// Coefficients are ordered ascending by power with power k at index k.
        /// Example: coefficients [3,-1,2] represent y=2x^2-x+3.
        /// </summary>
        /// <param name="z">The location where to evaluate the polynomial at.</param>
        /// <param name="coefficients">The coefficients of the polynomial, coefficient for power k at index k.</param>
        /// <exception cref="T:System.ArgumentNullException">
        /// <paramref name="coefficients" /> is a null reference.
        /// </exception>
        public static double PolynomialEvaluate(double z, params double[] coefficients) {
            int num1 = coefficients != null ? coefficients.Length : throw new ArgumentNullException(nameof(coefficients));
            if (num1 == 0)
                return 0.0;
            double num2 = coefficients[num1 - 1];
            for (int index = num1 - 2; index >= 0; --index)
                num2 = num2 * z + coefficients[index];
            return num2;
        }

        /// <summary>
        /// **************************************
        /// COEFFICIENTS FOR METHOD ErfImp       *
        /// **************************************
        /// </summary>
        /// <summary> Polynomial coefficients for a numerator of ErfImp
        /// calculation for Erf(x) in the interval [1e-10, 0.5].
        /// </summary>
        static readonly double[] ErfImpAn = new double[8] {
            0.0033791670955125737,
            -0.0007369565304816795,
            -0.3747323373929196,
            0.08174424487335873,
            -0.04210893199365486,
            0.007016570951209575,
            -0.004950912559824351,
            0.0008716465990379225
        };

        /// <summary> Polynomial coefficients for  a denominator of ErfImp
        /// calculation for Erf(x) in the interval [1e-10, 0.5].
        /// </summary>
        static readonly double[] ErfImpAd = new double[8] {
            1.0,
            -0.21808821808792464,
            0.4125429727254421,
            -0.08418911478731067,
            0.06553388564002416,
            -0.012001960445494177,
            0.00408165558926174,
            -0.0006159007215577697
        };

        /// <summary> Polynomial coefficients for a numerator in ErfImp
        /// calculation for Erfc(x) in the interval [0.5, 0.75].
        /// </summary>
        static readonly double[] ErfImpBn = new double[6] {
            -0.03617903907182625,
            0.2922518834448827,
            0.2814470417976045,
            0.12561020886276694,
            0.027413502826893053,
            0.0025083967216806575
        };

        /// <summary> Polynomial coefficients for a denominator in ErfImp
        /// calculation for Erfc(x) in the interval [0.5, 0.75].
        /// </summary>
        static readonly double[] ErfImpBd = new double[6] {
            1.0,
            1.8545005897903486,
            1.4357580303783142,
            0.5828276587530365,
            0.12481047693294975,
            0.011372417654635328
        };

        /// <summary> Polynomial coefficients for a numerator in ErfImp
        /// calculation for Erfc(x) in the interval [0.75, 1.25].
        /// </summary>
        static readonly double[] ErfImpCn = new double[7] {
            -0.03978768926111369,
            0.1531652124678783,
            0.19126029560093624,
            0.10276327061989304,
            0.029637090615738836,
            0.004609348678027549,
            0.0003076078203486802
        };

        /// <summary> Polynomial coefficients for a denominator in ErfImp
        /// calculation for Erfc(x) in the interval [0.75, 1.25].
        /// </summary>
        static readonly double[] ErfImpCd = new double[7] {
            1.0,
            1.955200729876277,
            1.6476231719938486,
            0.7682386070221262,
            0.20979318593650978,
            0.031956931689991336,
            0.0021336316089578537
        };

        /// <summary> Polynomial coefficients for a numerator in ErfImp
        /// calculation for Erfc(x) in the interval [1.25, 2.25].
        /// </summary>
        static readonly double[] ErfImpDn = new double[7] {
            -0.030083856055794972,
            0.05385788298444545,
            0.07262115416519142,
            0.036762846988804936,
            0.009646290155725275,
            0.0013345348007529107,
            7.780875997825043E-05
        };

        /// <summary> Polynomial coefficients for a denominator in ErfImp
        /// calculation for Erfc(x) in the interval [1.25, 2.25].
        /// </summary>
        static readonly double[] ErfImpDd = new double[8] {
            1.0,
            1.7596709814716753,
            1.3288357143796112,
            0.5525285965087576,
            0.13379305694133287,
            0.017950964517628076,
            0.0010471244001993736,
            -1.0664038182035734E-08
        };

        /// <summary> Polynomial coefficients for a numerator in ErfImp
        /// calculation for Erfc(x) in the interval [2.25, 3.5].
        /// </summary>
        static readonly double[] ErfImpEn = new double[7] {
            -0.011790757013722784,
            0.01426213209053881,
            0.020223443590296084,
            0.009306682999904321,
            0.00213357802422066,
            0.00025022987386460105,
            1.2053491221958819E-05
        };

        /// <summary> Polynomial coefficients for a denominator in ErfImp
        /// calculation for Erfc(x) in the interval [2.25, 3.5].
        /// </summary>
        static readonly double[] ErfImpEd = new double[7] {
            1.0,
            1.5037622520362048,
            0.9653977862044629,
            0.3392652304767967,
            0.06897406495415698,
            0.007710602624917683,
            0.0003714211015310693
        };

        /// <summary> Polynomial coefficients for a numerator in ErfImp
        /// calculation for Erfc(x) in the interval [3.5, 5.25].
        /// </summary>
        static readonly double[] ErfImpFn = new double[7] {
            -0.005469547955387293,
            0.004041902787317071,
            0.005496336955316117,
            0.002126164726039454,
            0.0003949840144950839,
            3.655654770644424E-05,
            1.3548589710993232E-06
        };

        /// <summary> Polynomial coefficients for a denominator in ErfImp
        /// calculation for Erfc(x) in the interval [3.5, 5.25].
        /// </summary>
        static readonly double[] ErfImpFd = new double[8] {
            1.0,
            1.2101969777363077,
            0.6209146682211439,
            0.17303843066114277,
            0.027655081377343203,
            0.0024062597442430973,
            8.918118172513365E-05,
            -4.655288362833827E-12
        };

        /// <summary> Polynomial coefficients for a numerator in ErfImp
        /// calculation for Erfc(x) in the interval [5.25, 8].
        /// </summary>
        static readonly double[] ErfImpGn = new double[6] {
            -0.0027072253590577837,
            0.00131875634250294,
            0.0011992593326100233,
            0.00027849619811344664,
            2.6782298821833186E-05,
            9.230436723150282E-07
        };

        /// <summary> Polynomial coefficients for a denominator in ErfImp
        /// calculation for Erfc(x) in the interval [5.25, 8].
        /// </summary>
        static readonly double[] ErfImpGd = new double[7] {
            1.0,
            0.8146328085431416,
            0.26890166585629954,
            0.044987721610304114,
            0.0038175966332024847,
            0.00013157189788859692,
            4.048153596757641E-12
        };

        /// <summary> Polynomial coefficients for a numerator in ErfImp
        /// calculation for Erfc(x) in the interval [8, 11.5].
        /// </summary>
        static readonly double[] ErfImpHn = new double[6] {
            -0.001099467206917422,
            0.00040642544275042267,
            0.0002744994894169007,
            4.652937706466594E-05,
            3.2095542539576746E-06,
            7.782860181450209E-08
        };

        /// <summary> Polynomial coefficients for a denominator in ErfImp
        /// calculation for Erfc(x) in the interval [8, 11.5].
        /// </summary>
        static readonly double[] ErfImpHd = new double[6] {
            1.0,
            0.5881737106118461,
            0.13936333128940975,
            0.016632934041708368,
            0.0010002392131023491,
            2.4254837521587224E-05
        };

        /// <summary> Polynomial coefficients for a numerator in ErfImp
        /// calculation for Erfc(x) in the interval [11.5, 17].
        /// </summary>
        static readonly double[] ErfImpIn = new double[5] {
            -0.0005690799360109496,
            0.00016949854037376225,
            5.184723545811009E-05,
            3.8281931223192885E-06,
            8.249899312818944E-08
        };

        /// <summary> Polynomial coefficients for a denominator in ErfImp
        /// calculation for Erfc(x) in the interval [11.5, 17].
        /// </summary>
        static readonly double[] ErfImpId = new double[6] {
            1.0,
            0.33963725005113937,
            0.04347264787031066,
            0.002485493352246371,
            5.356333053371529E-05,
            -1.1749094440545958E-13
        };

        /// <summary> Polynomial coefficients for a numerator in ErfImp
        /// calculation for Erfc(x) in the interval [17, 24].
        /// </summary>
        static readonly double[] ErfImpJn = new double[5] {
            -0.00024131359948399134,
            5.742249752025015E-05,
            1.1599896292738377E-05,
            5.817621344025938E-07,
            8.539715550856736E-09
        };

        /// <summary> Polynomial coefficients for a denominator in ErfImp
        /// calculation for Erfc(x) in the interval [17, 24].
        /// </summary>
        static readonly double[] ErfImpJd = new double[5] {
            1.0,
            0.23304413829968784,
            0.02041869405464403,
            0.0007971856475643983,
            1.1701928167017232E-05
        };

        /// <summary> Polynomial coefficients for a numerator in ErfImp
        /// calculation for Erfc(x) in the interval [24, 38].
        /// </summary>
        static readonly double[] ErfImpKn = new double[5] {
            -0.00014667469927776036,
            1.6266655211228053E-05,
            2.6911624850916523E-06,
            9.79584479468092E-08,
            1.0199464762572346E-09
        };

        /// <summary> Polynomial coefficients for a denominator in ErfImp
        /// calculation for Erfc(x) in the interval [24, 38].
        /// </summary>
        static readonly double[] ErfImpKd = new double[5] {
            1.0,
            0.16590781294484722,
            0.010336171619150588,
            0.0002865930263738684,
            2.9840157084090034E-06
        };

        /// <summary> Polynomial coefficients for a numerator in ErfImp
        /// calculation for Erfc(x) in the interval [38, 60].
        /// </summary>
        static readonly double[] ErfImpLn = new double[5] {
            -5.839057976297718E-05,
            4.125103251054962E-06,
            4.3179092242025094E-07,
            9.933651555900132E-09,
            6.534805100201047E-11
        };

        /// <summary> Polynomial coefficients for a denominator in ErfImp
        /// calculation for Erfc(x) in the interval [38, 60].
        /// </summary>
        static readonly double[] ErfImpLd = new double[5] {
            1.0,
            0.10507708607203992,
            0.004142784286754756,
            7.263387546445238E-05,
            4.778184710473988E-07
        };

        /// <summary> Polynomial coefficients for a numerator in ErfImp
        /// calculation for Erfc(x) in the interval [60, 85].
        /// </summary>
        static readonly double[] ErfImpMn = new double[4] {
            -1.9645779760922958E-05,
            1.572438876668007E-06,
            5.439025111927009E-08,
            3.174724923691177E-10
        };

        /// <summary> Polynomial coefficients for a denominator in ErfImp
        /// calculation for Erfc(x) in the interval [60, 85].
        /// </summary>
        static readonly double[] ErfImpMd = new double[5] {
            1.0,
            0.05280398924095763,
            0.0009268760691517533,
            5.410117232266303E-06,
            5.350938458036424E-16
        };

        /// <summary> Polynomial coefficients for a numerator in ErfImp
        /// calculation for Erfc(x) in the interval [85, 110].
        /// </summary>
        static readonly double[] ErfImpNn = new double[4] {
            -7.892247039787227E-06,
            6.22088451660987E-07,
            1.457284456768824E-08,
            6.037155055427153E-11
        };

        /// <summary> Polynomial coefficients for a denominator in ErfImp
        /// calculation for Erfc(x) in the interval [85, 110].
        /// </summary>
        static readonly double[] ErfImpNd = new double[4] {
            1.0,
            0.03753288463562937,
            0.0004679195359746253,
            1.9384703927584565E-06
        };

        /// <summary>
        /// **************************************
        /// COEFFICIENTS FOR METHOD ErfInvImp    *
        /// **************************************
        /// </summary>
        /// <summary> Polynomial coefficients for a numerator of ErfInvImp
        /// calculation for Erf^-1(z) in the interval [0, 0.5].
        /// </summary>
        static readonly double[] ErvInvImpAn = new double[8] {
            -0.0005087819496582806,
            -0.008368748197417368,
            0.03348066254097446,
            -0.012692614766297404,
            -0.03656379714117627,
            0.02198786811111689,
            0.008226878746769157,
            -0.005387729650712429
        };

        /// <summary> Polynomial coefficients for a denominator of ErfInvImp
        /// calculation for Erf^-1(z) in the interval [0, 0.5].
        /// </summary>
        static readonly double[] ErvInvImpAd = new double[10] {
            1.0,
            -0.9700050433032906,
            -1.5657455823417585,
            1.5622155839842302,
            0.662328840472003,
            -0.7122890234154284,
            -0.05273963823400997,
            0.07952836873415717,
            -0.0023339375937419,
            0.0008862163904564247
        };

        /// <summary> Polynomial coefficients for a numerator of ErfInvImp
        /// calculation for Erf^-1(z) in the interval [0.5, 0.75].
        /// </summary>
        static readonly double[] ErvInvImpBn = new double[9] {
            -0.20243350835593876,
            0.10526468069939171,
            8.3705032834312,
            17.644729840837403,
            -18.851064805871424,
            -44.6382324441787,
            17.445385985570866,
            21.12946554483405,
            -3.6719225470772936
        };

        /// <summary> Polynomial coefficients for a denominator of ErfInvImp
        /// calculation for Erf^-1(z) in the interval [0.5, 0.75].
        /// </summary>
        static readonly double[] ErvInvImpBd = new double[9] {
            1.0,
            6.242641248542475,
            3.971343795334387,
            -28.66081804998,
            -20.14326346804852,
            48.560921310873994,
            10.826866735546016,
            -22.643693341313973,
            1.7211476576120028
        };

        /// <summary> Polynomial coefficients for a numerator of ErfInvImp
        /// calculation for Erf^-1(z) in the interval [0.75, 1] with x less than 3.
        /// </summary>
        static readonly double[] ErvInvImpCn = new double[11] {
            -0.1311027816799519,
            -0.16379404719331705,
            0.11703015634199525,
            0.38707973897260434,
            0.3377855389120359,
            0.14286953440815717,
            0.029015791000532906,
            0.0021455899538880526,
            -6.794655751811263E-07,
            2.8522533178221704E-08,
            -6.81149956853777E-10
        };

        /// <summary> Polynomial coefficients for a denominator of ErfInvImp
        /// calculation for Erf^-1(z) in the interval [0.75, 1] with x less than 3.
        /// </summary>
        static readonly double[] ErvInvImpCd = new double[8] {
            1.0,
            3.4662540724256723,
            5.381683457070069,
            4.778465929458438,
            2.5930192162362027,
            0.848854343457902,
            0.15226433829533179,
            0.011059242293464892
        };

        /// <summary> Polynomial coefficients for a numerator of ErfInvImp
        /// calculation for Erf^-1(z) in the interval [0.75, 1] with x between 3 and 6.
        /// </summary>
        static readonly double[] ErvInvImpDn = new double[9] {
            -0.0350353787183178,
            -0.0022242652921344794,
            0.018557330651423107,
            0.009508047013259196,
            0.0018712349281955923,
            0.00015754461742496055,
            4.60469890584318E-06,
            -2.304047769118826E-10,
            2.6633922742578204E-12
        };

        /// <summary> Polynomial coefficients for a denominator of ErfInvImp
        /// calculation for Erf^-1(z) in the interval [0.75, 1] with x between 3 and 6.
        /// </summary>
        static readonly double[] ErvInvImpDd = new double[7] {
            1.0,
            1.3653349817554064,
            0.7620591645536234,
            0.22009110576413124,
            0.03415891436709477,
            0.00263861676657016,
            7.646752923027944E-05
        };

        /// <summary> Polynomial coefficients for a numerator of ErfInvImp
        /// calculation for Erf^-1(z) in the interval [0.75, 1] with x between 6 and 18.
        /// </summary>
        static readonly double[] ErvInvImpEn = new double[9] {
            -0.016743100507663373,
            -0.0011295143874558028,
            0.001056288621524929,
            0.00020938631748758808,
            1.4962478375834237E-05,
            4.4969678992770644E-07,
            4.625961635228786E-09,
            -2.811287356288318E-14,
            9.905570997331033E-17
        };

        /// <summary> Polynomial coefficients for a denominator of ErfInvImp
        /// calculation for Erf^-1(z) in the interval [0.75, 1] with x between 6 and 18.
        /// </summary>
        static readonly double[] ErvInvImpEd = new double[7] {
            1.0,
            0.5914293448864175,
            0.1381518657490833,
            0.016074608709367652,
            0.0009640118070051656,
            2.7533547476472603E-05,
            2.82243172016108E-07
        };

        /// <summary> Polynomial coefficients for a numerator of ErfInvImp
        /// calculation for Erf^-1(z) in the interval [0.75, 1] with x between 18 and 44.
        /// </summary>
        static readonly double[] ErvInvImpFn = new double[8] {
            -0.002497821279189813,
            -7.79190719229054E-06,
            2.5472303741302746E-05,
            1.6239777734251093E-06,
            3.963410113048012E-08,
            4.116328311909442E-10,
            1.4559628671867504E-12,
            -1.1676501239718427E-18
        };

        /// <summary> Polynomial coefficients for a denominator of ErfInvImp
        /// calculation for Erf^-1(z) in the interval [0.75, 1] with x between 18 and 44.
        /// </summary>
        static readonly double[] ErvInvImpFd = new double[7] {
            1.0,
            0.2071231122144225,
            0.01694108381209759,
            0.0006905382656226846,
            1.4500735981823264E-05,
            1.4443775662814415E-07,
            5.097612765997785E-10
        };

        /// <summary> Polynomial coefficients for a numerator of ErfInvImp
        /// calculation for Erf^-1(z) in the interval [0.75, 1] with x greater than 44.
        /// </summary>
        static readonly double[] ErvInvImpGn = new double[8] {
            -0.0005390429110190785,
            -2.8398759004727723E-07,
            8.994651148922914E-07,
            2.2934585926592085E-08,
            2.2556144486350015E-10,
            9.478466275030226E-13,
            1.3588013010892486E-15,
            -3.4889039339994887E-22
        };

        /// <summary> Polynomial coefficients for a denominator of ErfInvImp
        /// calculation for Erf^-1(z) in the interval [0.75, 1] with x greater than 44.
        /// </summary>
        static readonly double[] ErvInvImpGd = new double[7] {
            1.0,
            0.08457462340018994,
            0.002820929847262647,
            4.682929219408942E-05,
            3.999688121938621E-07,
            1.6180929088790448E-09,
            2.315586083102596E-12
        };
    }
}