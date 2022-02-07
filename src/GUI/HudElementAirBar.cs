using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Client;

namespace GasApi
{
    public class HudElementAirBar : HudElement
    {
        GuiElementStatbar statbar;

        public HudElementAirBar(ICoreClientAPI capi) : base(capi)
        {
        }

        public override void OnOwnPlayerDataReceived()
        {
            ElementBounds statbarbounds = ElementStdBounds.Statbar(EnumDialogArea.CenterBottom, 347).WithFixedAlignmentOffset(124, -44);
            statbarbounds.WithFixedHeight(9);

            ElementBounds parentBounds = statbarbounds.ForkBoundingParent();

            SingleComposer = capi.Gui.CreateCompo("airbar", parentBounds)
                .AddStatbar(statbarbounds, new double[] { 0.2, 0.2, 0.2, 0.5 }, "background")
                .AddInvStatbar(statbarbounds, new double[] { 0, 0.4, 0.5, 0.5 }, "airbar")
                .Compose();
            SingleComposer.GetStatbar("background").SetMinMax(0, 1);

            statbar = SingleComposer.GetStatbar("airbar");
            statbar.SetMinMax(0, 15);
            statbar.SetLineInterval(1);
            statbar.FlashTime = 4.0f;

            SingleComposer.ReCompose();

            capi.World.Player.Entity.WatchedAttributes.RegisterModifiedListener("air", () => UpdateGUI());

            base.OnOwnPlayerDataReceived();
        }

        public void UpdateGUI()
        {
            EntityPlayer player = capi.World.Player.Entity;


            ITreeAttribute air = player.WatchedAttributes.GetTreeAttribute("air");
            float current = 0;
            float max = 0;

            if (air != null)
            {
                current = air.GetFloat("currentair");
                max = air.GetFloat("maxair");
            }

            statbar.SetMinMax(0, max);
            statbar.SetLineInterval(1);

            if (air != null && current != statbar.GetValue())
            {
                statbar.SetValue(current);
            }
        }

        public override void OnRenderGUI(float deltaTime)
        {
            statbar.ShouldFlash = statbar.GetValue() < 0.5 ? true : false;
            statbar.Enabled = false;
            base.OnRenderGUI(deltaTime);
        }
    }
}
