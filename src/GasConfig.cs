namespace GasApi
{
    public class GasConfig
    {
        public static GasConfig Loaded { get; set; } = new GasConfig();

        //Gas settings

        public bool Explosions { get; set; } = true;

        public bool FlammableGas { get; set; } = true;

        public double PickaxeExplosionChance { get; set; } = 0.25;

        public bool ContainerBonus { get; set; } = true;

        public bool Smoke { get; set; } = true;

        public bool Acid { get; set; } = true;      

        public bool Exhaling { get; set; } = true;

        public int DefaultSpreadRadius { get; set; } = 7;

        public bool SpreadGasOnBreak { get; set; } = true;

        public bool SpreadGasOnPlace { get; set; } = false;

        public float UpdateSpreadGasChance { get; set; } = 0.01f;

        //Breathing Settings

        public bool AllowScuba { get; set; } = true;

        public bool AllowMasks { get; set; } = true;

        public bool ToxicEffects { get; set; } = true;

        #region Control Content

        public bool GasesEnabled { get; set; } = true;

        public bool GasesDebugEnabled { get; set; } = true;

        public bool BreathingEnabled { get; set; } = true;

        public bool PlayerBreathingEnabled { get; set; } = true;
        #endregion
    }
}
