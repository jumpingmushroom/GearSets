using System;
using GearSets.Core.Model;

namespace GearSets.Core
{
    /// <summary>"gearsets" console command; output is mirrored to the BepInEx log for build/logs.sh.</summary>
    internal static class ConsoleCommands
    {
        public static void Register()
        {
            new Terminal.ConsoleCommand("gearsets", "GearSets: list | save <name> | equip <name> | delete <name> | dump",
                delegate (Terminal.ConsoleEventArgs args)
                {
                    try
                    {
                        Run(args);
                    }
                    catch (Exception e)
                    {
                        Say(args.Context, "GearSets: command failed: " + e.Message);
                        GearSetsPlugin.WarnOnce("GearSets: console command failed", e);
                    }
                });
        }

        private static void Run(Terminal.ConsoleEventArgs args)
        {
            Terminal ctx = args.Context;
            Player p = Player.m_localPlayer;
            if (p == null)
            {
                Say(ctx, "GearSets: not in a game.");
                return;
            }
            string sub = args.Length > 1 ? args[1].ToLowerInvariant() : "list";
            string rest = args.Length > 2 ? string.Join(" ", args.Args, 2, args.Length - 2) : "";
            switch (sub)
            {
                case "save":
                {
                    string error;
                    GearSet s = SetActions.SaveNew(rest, null, CaptureMask.All(), out error);
                    Say(ctx, s != null ? "GearSets: saved " + s.Name + "." : "GearSets: " + error);
                    break;
                }
                case "equip":
                {
                    GearSet s = SetStore.Book.FindByName(rest);
                    if (s == null)
                        Say(ctx, "GearSets: no set named " + rest + ".");
                    else
                        SwapExecutor.Equip(s);
                    break;
                }
                case "delete":
                {
                    GearSet s = SetStore.Book.FindByName(rest);
                    if (s == null)
                    {
                        Say(ctx, "GearSets: no set named " + rest + ".");
                        break;
                    }
                    SetActions.Delete(s);
                    Say(ctx, "GearSets: deleted " + s.Name + ".");
                    break;
                }
                case "dump":
                {
                    Snapshot snap = Snapshotter.Take(p);
                    Say(ctx, "GearSets: grid " + snap.Facts.Width + " wide, " + snap.Facts.PlayerRows + " player rows, " +
                        snap.Facts.UtilitySlots + " utility slots.");
                    foreach (ItemFacts f in snap.Facts.Items)
                        Say(ctx, "GearSets:   " + f.Prefab + " (" + f.X + "," + f.Y + ") fits " + f.Fits + " worn " + f.Worn +
                            " queued " + f.Queued + " q" + f.Quality + " dur " + f.Durability.ToString("0") + " tag " + (f.Tag ?? "-"));
                    break;
                }
                default:
                    Say(ctx, "GearSets: " + SetStore.Book.Sets.Count + " sets.");
                    foreach (GearSet s in SetStore.Book.Sets)
                        Say(ctx, "GearSets:   " + s.Name + " (icon " + s.Icon + ", hotbar " + (s.HasHotbar ? "yes" : "no") + ")");
                    break;
            }
        }

        internal static void Say(Terminal ctx, string line)
        {
            if (ctx != null)
                ctx.AddString(line);
            GearSetsPlugin.Log.LogInfo(line);
        }
    }
}
