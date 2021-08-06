using BOMjak.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace BOMjak.Console
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Directory.Delete("img", true);

            var manager = new BOMJakManager(Core.Model.LocationCode.IDR023);

            await (await manager.CreateAnimatedAsync()).DisposeAsync();
        }
    }
}
