using OpenRA.Mods.Common.Widgets;
using OpenRA.Mods.Common.Widgets.Logic;
using OpenRA.Mods.MW.Traits.Orders;
using OpenRA.Widgets;

namespace OpenRA.Mods.MW.Widgets.Logic.Ingame
{
    public class MwSellOrderButtonLogic : ChromeLogic
    {
        [ObjectCreator.UseCtor]
        public MwSellOrderButtonLogic(Widget widget, World world)
        {
            var sell = widget as ButtonWidget;
            if (sell != null)
                OrderButtonsChromeUtils.BindOrderButton<SellOrderGenerator>(world, sell, "sell");
        }
    }
}