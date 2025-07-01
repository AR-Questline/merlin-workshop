using Awaken.TG.Code.Utility;
using Awaken.Utility.Enums;
using CrazyMinnow.SALSA;

namespace Awaken.TG.Main.AudioSystem {
    public class SalsaEmotion : RichEnum {
        readonly int[] _indexes;

        public int Index => _indexes.Length > 0 ? RandomUtil.UniformSelect(_indexes) : _indexes[0];
        public ExpressionComponent.ExpressionHandler ExpressionHandler { get; private set; }
        
        SalsaEmotion(string enumName, int[] indexes, ExpressionComponent.ExpressionHandler expressionHandler,
            string inspectorCategory = "") : base(enumName, inspectorCategory) {
            _indexes = indexes;
            ExpressionHandler = expressionHandler;
        }

        public static readonly SalsaEmotion
            Attack = new(nameof(Attack), new[] { 0, 1 }, ExpressionComponent.ExpressionHandler.RoundTrip),
            Hurt = new(nameof(Hurt), new[] { 2, 3 }, ExpressionComponent.ExpressionHandler.RoundTrip),
            Dead = new(nameof(Dead), new[] { 4 }, ExpressionComponent.ExpressionHandler.OneWay);
    }
}