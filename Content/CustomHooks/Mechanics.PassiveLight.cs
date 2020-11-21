﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using StarlightRiver.Core;
using StarlightRiver.Physics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.Graphics.Effects;
using Terraria.ID;
using Terraria.ModLoader;
using static Terraria.ModLoader.ModContent;

namespace StarlightRiver.Content.CustomHooks
{
    class PassiveLight : HookGroup
    {
        //Rare method to hook but not the best finding logic. Also old code.
        public override SafetyLevel Safety => SafetyLevel.Fragile;

        public override void Load()
        {
            IL.Terraria.Lighting.PreRenderPhase += VitricLighting;
        }

        public override void Unload()
        {
            IL.Terraria.Lighting.PreRenderPhase -= VitricLighting;
        }

        private delegate void ModLightingStateDelegate(float from, ref float to);
        private delegate void ModColorDelegate(int i, int j, ref float r, ref float g, ref float b);

        private void VitricLighting(ILContext il)
        {
            // Create our cursor at the start of the void PreRenderPhase() method.
            ILCursor c = new ILCursor(il);

            // We insert our emissions right before the ModifyLight call (line 1963, CIL 0x3428)
            // Get the TileLoader.ModifyLight method. Then, using it,
            // find where it's called and place the cursor right before that call instruction.

            MethodInfo ModifyLight = typeof(TileLoader).GetMethod("ModifyLight", BindingFlags.Public | BindingFlags.Static);
            c.GotoNext(i => i.MatchCall(ModifyLight));

            // Emit the values of I and J.
            /* To emit local variables, you have to know the indeces of where those variables are stored.
             * These are stated at the very top of the method, in a format like below:
             * .locals init ( 
             *      [0] = float32 FstName, 
             *      [1] = ScdName, 
             *      [2] = ThdName
             * )
            */

            c.Emit(OpCodes.Ldloc, 27); // [27] = n
            c.Emit(OpCodes.Ldloc, 29); // [29] = num17

            /* Emit the addresses of R, G, and B.
             * It's important to emit their *addresses*, because we're passing them—
             *   by reference, not by value. Under the hood, "ref" tokens—
             *   pass a pointer to the object (even for managed types),
             *   and that's what we need to do here.
            */
            c.Emit(OpCodes.Ldloca, 32); // [32] = num18
            c.Emit(OpCodes.Ldloca, 33); // [33] = num19
            c.Emit(OpCodes.Ldloca, 34); // [34] = num20

            // Consume the values of I,J and the addresses of R,G,B by calling EmitVitricDel.
            c.EmitDelegate<ModColorDelegate>(EmitVitricDel);
        }

        private static void EmitVitricDel(int i, int j, ref float r, ref float g, ref float b)
        {
            if (Main.tile[i, j] == null)
            {
                return;
            }
            // If the tile is in the vitric biome and doesn't block light, emit light.
            bool tileBlock = Main.tile[i, j].active() && Main.tileBlockLight[Main.tile[i, j].type] && !(Main.tile[i, j].slope() != 0 || Main.tile[i, j].halfBrick());
            bool wallBlock = Main.wallLight[Main.tile[i, j].wall];
            if (StarlightWorld.VitricBiome.Contains(i, j) && Main.tile[i, j] != null && !tileBlock && wallBlock)
            {
                r = .4f;
                g = .57f;
                b = .65f;
            }

            //underworld lighting
            if (Vector2.Distance(Main.LocalPlayer.Center, StarlightWorld.RiftLocation) <= 1500 && j >= Main.maxTilesY - 200 && Main.tile[i, j] != null && !tileBlock && wallBlock)
            {
                r = 0;
                g = 0;
                b = (1500 / Vector2.Distance(Main.LocalPlayer.Center, StarlightWorld.RiftLocation) - 1) / 2;
                if (b >= 0.8f) b = 0.8f;
            }

            //I really need to stop bloating this method so much
            if (Main.LocalPlayer.GetModPlayer<BiomeHandler>().zoneAshhell && !tileBlock && wallBlock)
            {
                r = 0.0f;
                g = 0.4f;
                b = 0.3f;
            }

            //waters, probably not the most amazing place to do this but it works and dosent melt people's PCs
            if (!tileBlock && Main.tile[i, j].liquid != 0 && Main.tile[i, j].liquidType() == 0)
            {
                //the crimson
                if (Main.LocalPlayer.GetModPlayer<BiomeHandler>().ZoneJungleBloody || Main.LocalPlayer.GetModPlayer<BiomeHandler>().FountainJungleBloody)
                {
                    if (Main.tile[i, j - 1].liquid != 0 || Main.tile[i, j - 1].active())
                    {
                        r = 0.25f;
                        g = 0.14f;
                        b = 0.0f;

                        if (Main.rand.Next(40) == 0)
                            Dust.NewDustPerfect(new Vector2(i * 16 + Main.rand.Next(16), j * 16 + Main.rand.Next(16)), ModContent.DustType<Dusts.Stamina>(), new Vector2(0, Main.rand.NextFloat(-0.8f, -0.6f)), 0, default, 0.6f);
                    }
                    else
                    {
                        r = 0.4f;
                        g = 0.32f;
                        b = 0.0f;
                        if (Main.rand.Next(5) == 0)
                            Dust.NewDustPerfect(new Vector2(i * 16 + Main.rand.Next(16), j * 16 + Main.rand.Next(16)), ModContent.DustType<Dusts.Gold2>(), new Vector2(0, Main.rand.NextFloat(-1.4f, -1.2f)), 0, default, 0.3f);
                    }
                }
                // the corruption
                if (Main.LocalPlayer.GetModPlayer<BiomeHandler>().ZoneJungleCorrupt || Main.LocalPlayer.GetModPlayer<BiomeHandler>().FountainJungleCorrupt)
                {
                    if (Main.tile[i, j - 1].liquid != 0 || Main.tile[i, j - 1].active())
                    {
                        if (Main.rand.Next(80) == 0)
                            Dust.NewDustPerfect(new Vector2(i * 16 + Main.rand.Next(16), j * 16 + Main.rand.Next(16)), 186, new Vector2(0, Main.rand.NextFloat(0.6f, 0.8f)), 0, default, 1f);
                    }
                    else
                    {
                        r = 0.1f;
                        g = 0.1f;
                        b = 0.3f;
                        if (Main.rand.Next(10) == 0)
                            Dust.NewDustPerfect(new Vector2(i * 16 + Main.rand.Next(16), j * 16 - Main.rand.Next(-20, 20)), 112, new Vector2(0, Main.rand.NextFloat(1.2f, 1.4f)), 120, new Color(100, 100, 200) * 0.6f, 0.6f);
                    }
                }
                //the hallow
                if (Main.LocalPlayer.GetModPlayer<BiomeHandler>().ZoneJungleHoly || Main.LocalPlayer.GetModPlayer<BiomeHandler>().FountainJungleHoly)
                {
                    if (Main.tile[i, j - 1].liquid != 0 || Main.tile[i, j - 1].active())
                    {
                        r = 0.1f;
                        g = 0.3f;
                        b = 0.3f;
                        if (Main.rand.Next(80) == 0)
                            Dust.NewDustPerfect(new Vector2(i * 16 + Main.rand.Next(16), j * 16 + Main.rand.Next(16)), ModContent.DustType<Dusts.Starlight>(), Vector2.One.RotatedByRandom(6.28f) * 10, 0, default, 0.5f);
                    }
                    else
                    {
                        r = 0.1f;
                        g = 0.5f;
                        b = 0.3f;
                        if (Main.rand.Next(100) == 0)
                            Dust.NewDustPerfect(new Vector2(i * 16 + Main.rand.Next(16), j * 16 + Main.rand.Next(-1, 20)), ModContent.DustType<Dusts.AirDash>(), new Vector2(0, Main.rand.NextFloat(-1, -0.1f)), 120, default, Main.rand.NextFloat(1.1f, 2.4f));
                    }
                }

            }

            //trees, 100% not the right place to do this. I should probably move this later. I wont. Kill me.
            if (Main.tile[i, j].type == TileID.Trees && Main.tile[i, j - 1].type != TileID.Trees && Main.tile[i, j + 1].type == TileID.Trees
                && Helper.ScanForTypeDown(i, j, ModContent.TileType<Tiles.JungleHoly.GrassJungleHoly>())) //at the top of trees in the holy jungle
            {
                Color color = new Color();
                switch (i % 3)
                {
                    case 0: color = new Color(150, 255, 230); break;
                    case 1: color = new Color(255, 180, 255); break;
                    case 2: color = new Color(200, 150, 255); break;
                }

                if (Main.rand.Next(5) == 0)
                {
                    Dust d = Dust.NewDustPerfect(new Vector2(i, j - 3) * 16 + Vector2.One.RotatedByRandom(6.28f) * Main.rand.NextFloat(32), ModContent.DustType<Dusts.BioLumen>(), new Vector2(0.9f, 0.3f), 0, color, 1);
                    d.fadeIn = Main.rand.NextFloat(3.14f);
                }
                r = color.R / 555f; //lazy value tuning
                g = color.G / 555f;
                b = color.B / 555f;
            }
        }
    }
}