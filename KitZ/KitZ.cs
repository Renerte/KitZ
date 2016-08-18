using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using TerrariaApi.Server;

namespace KitZ
{
    public class KitZ : TerrariaPlugin
    {
        public KitZ(Main game) : base(game)
        {
        }

        public override void Initialize()
        {
            throw new NotImplementedException();
        }

        public override string Author { get { return "Renerte"; } }
        public override string Description { get { return "Customizable kits for your TShock server!"; } }
        public override string Name { get { return "KitZ"; } }
        public override Version Version { get { return Assembly.GetExecutingAssembly().GetName().Version; } }
    }
}
