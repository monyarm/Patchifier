#region

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Cache.Implementations;
using Mutagen.Bethesda.Skyrim;
using Mutagen.Bethesda.Synthesis;
using Noggog;
// ReSharper disable All

#endregion
#pragma warning disable CA1416 // Validate platform compatibility

namespace Patchifier
{
    public class Program
    {
        private static Lazy<Settings.Settings> _settings = null!;

        public static async Task<int> Main(string[] args)
        {

            return await SynthesisPipeline.Instance
                .AddPatch<ISkyrimMod, ISkyrimModGetter>(RunPatch)
                .SetAutogeneratedSettings<Settings.Settings>("settings", "settings.json", out _settings)
                .SetTypicalOpen(GameRelease.SkyrimSE, "Patchifier.esp")
                .Run(args);

        }
        public static void RunPatch(IPatcherState<ISkyrimMod, ISkyrimModGetter> state)
        {
            var priorityOrder = state.LoadOrder.PriorityOrder;

            var preSynthOrder = priorityOrder.Where(x => !x.ModKey.Name.ContainsInsensitive("Synthesis"));
            var preSynthCache = preSynthOrder.ToImmutableLinkCache();

            string skyPatcherNpcs = "";

            skyPatcherNpcs = HandleNpcs(state, preSynthCache, skyPatcherNpcs);

            if (skyPatcherNpcs != "")
            {
                string path = Path.Combine(Path.GetDirectoryName(state.OutputPath) ?? "", "SKSE", "Plugins", "SkyPatcher", "npc", "SynthNpcs.ini");
                if (!Directory.Exists(Path.GetDirectoryName(path)))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(path) ?? "");
                }
                File.WriteAllText(path, skyPatcherNpcs);
            }
        }

        private static string HandleNpcs(IPatcherState<ISkyrimMod, ISkyrimModGetter> state, ImmutableLoadOrderLinkCache<ISkyrimMod, ISkyrimModGetter> preSynthCache, string skyPatcherNpcs)
        {
            foreach (var npc in state.PatchMod.Npcs)
            {
                INpcGetter npcPreSynth = preSynthCache.Resolve<INpcGetter>(npc.FormKey);
                Npc.Mask<bool> mask = npc.GetEqualsMask(npcPreSynth);
                if ((new bool[] {
                    mask.VirtualMachineAdapter!.Overall,
                    mask.ObjectBounds!.Overall,
                    // mask.Configuration!.Overall,
                    mask.Configuration!.Specific!.BleedoutOverride,
                    mask.Configuration!.Specific!.CalcMaxLevel,
                    mask.Configuration!.Specific!.CalcMinLevel,
                    mask.Configuration!.Specific!.DispositionBase,
                    mask.Configuration!.Specific!.HealthOffset,
                    mask.Configuration!.Specific!.MagickaOffset,
                    mask.Configuration!.Specific!.SpeedMultiplier,
                    mask.Configuration!.Specific!.StaminaOffset,
                    mask.Configuration!.Specific!.TemplateFlags,
                    mask.Factions!.Overall,
                    mask.DeathItem,
                    mask.Voice,
                    mask.Template,
                    mask.Race,
                    mask.ActorEffect!.Overall,
                    mask.Destructible!.Overall,
                    mask.WornArmor,
                    mask.FarAwayModel,
                    mask.AttackRace,
                    mask.Attacks!.Overall,
                    mask.SpectatorOverridePackageList,
                    mask.ObserveDeadBodyOverridePackageList,
                    mask.GuardWarnOverridePackageList,
                    mask.CombatOverridePackageList,
                    mask.Perks!.Overall,
                    mask.Items!.Overall,
                    mask.AIData!.Overall,
                    mask.Packages!.Overall,
                    mask.Keywords!.Overall,
                    mask.Class,
                    mask.Name,
                    mask.ShortName,
                    mask.PlayerSkills!.Overall,
                    mask.HeadParts!.Overall,
                    mask.HairColor,
                    mask.CombatStyle,
                    mask.GiftFilter,
                    mask.NAM5,
                    mask.Height,
                    mask.Weight,
                    mask.SoundLevel,
                    mask.Sound!.Overall,
                    mask.DefaultOutfit,
                    mask.SleepingOutfit,
                    mask.DefaultPackageList,
                    mask.CrimeFaction,
                    mask.HeadTexture,
                    mask.TextureLighting,
                    mask.FaceMorph!.Overall,
                    mask.FaceParts!.Overall,
                    mask.TintLayers!.Overall,
                    mask.MajorRecordFlagsRaw,
                    mask.FormVersion,
                    mask.Version2,
                    mask.EditorID,
                    mask.FormKey,
                    mask.VersionControl,}).Any(x => !x))
                {
                    continue;
                }
                else
                {
                    if (!mask.Configuration.Specific.Flags)
                    {
                        var allowedFlags = NpcConfiguration.Flag.AutoCalcStats & NpcConfiguration.Flag.Essential & NpcConfiguration.Flag.Protected;
                        var flagDiff = npc.Configuration.Flags ^ npcPreSynth.Configuration.Flags;
                        if ((flagDiff & allowedFlags) != ~ allowedFlags )
                        {
                            continue;
                        }
                        else
                        {
                            skyPatcherNpcs += $"\n;{npc.EditorID} \"{npc.Name}\" [{npc.FormKey.ModKey.Name}|{npc.FormKey.IDString()}]";
                            if (flagDiff.HasFlag(NpcConfiguration.Flag.AutoCalcStats))
                            {
                                skyPatcherNpcs += $"\nfilterByNpcs={npc.FormKey.ModKey.Name}|{npc.FormKey.IDString()}:setAutoCalcStats={npc.Configuration.Flags.HasFlag(NpcConfiguration.Flag.AutoCalcStats)}";
                            }
                            if (flagDiff.HasFlag(NpcConfiguration.Flag.Essential))
                            {
                                skyPatcherNpcs += $"\nfilterByNpcs={npc.FormKey.ModKey.Name}|{npc.FormKey.IDString()}:setEssential={npc.Configuration.Flags.HasFlag(NpcConfiguration.Flag.AutoCalcStats)}";
                            }
                            if (flagDiff.HasFlag(NpcConfiguration.Flag.Protected))
                            {
                                skyPatcherNpcs += $"\nfilterByNpcs={npc.FormKey.ModKey.Name}|{npc.FormKey.IDString()}:setProtected={npc.Configuration.Flags.HasFlag(NpcConfiguration.Flag.AutoCalcStats)}";
                            }
                            skyPatcherNpcs += $"\n";

                            state.PatchMod.Npcs.Remove(npc);
                        }
                    }
                }

            }

            return skyPatcherNpcs;
        }
    }
}
