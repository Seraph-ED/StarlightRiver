﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StarlightRiver.Content.Buffs;
using StarlightRiver.Content.Dusts;
using StarlightRiver.Core;
using StarlightRiver.Helpers;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.GameInput;
using Terraria.ID;
using Terraria.ModLoader;
using static Terraria.ModLoader.ModContent;

namespace StarlightRiver.Content.Abilities.Faeflame
{
	public class Whip : Ability
    {
        public override string Texture => "StarlightRiver/Assets/Abilities/Faeflame";
        public override float ActivationCostDefault => 0.15f;
        public override Color Color => new Color(255, 247, 126);

        public Vector2 endPoint; //where the "tip" of the whip is in the world
        public bool attached; //if the whip is attached to anything
        public bool endRooted; //if the endpoint is "rooted" to a certain location and cant be moved

        public float length;

        public Vector2 extraVelocity;
        public float targetRot;

        public NPC attachedNPC; //if the whip is attached to an npc, what is it attached to?

        public override void Reset()
        {

        }

        public override void OnActivate()
        {
            Player.mount.Dismount(Player);

            targetRot = (Main.MouseWorld - Player.Center).ToRotation();
            endPoint = Player.Center;
        }

        public override void UpdateActive()
        {
            bool control = StarlightRiver.Instance.AbilityKeys.Get<Whip>().Current;

            if (!control || Player.GetHandler().Stamina <= 0)
            {
                endRooted = false;
                attached = false;
                attachedNPC = null;

                Deactivate();

                extraVelocity = Main.MouseScreen;
                return;
            }

            Player.GetHandler().Stamina -= 0.005f;

            if (!endRooted)
            {
                var dist = Vector2.Distance(Player.Center, endPoint);

                for (int k = 0; k < 4; k++)
                {
                    if (dist < 450)
                        endPoint += Vector2.UnitX.RotatedBy(targetRot) * 16;

                    if (Framing.GetTileSafely((int)endPoint.X / 16, (int)endPoint.Y / 16).collisionType == 1) //debug
                        endRooted = true;
                }

                length = dist - 80;
                if (length < 100)
                    length = 100;
            }
            else
            {
                if (attachedNPC != null && attachedNPC.active)
                    endPoint = attachedNPC.Center;

                Player.velocity -= extraVelocity;

                Player.velocity.Y -= 0.43f;

                Player.velocity += (Main.MouseWorld - endPoint) * -(0.05f - Helper.BezierEase(Player.velocity.Length() / 24f) * 0.025f);

                if (Player.velocity.Length() > 18)
                    Player.velocity = Vector2.Normalize(Player.velocity) * 17.99f;

                Player.velocity *= 0.92f;

                Vector2 pullPoint = endPoint + Vector2.Normalize(Player.Center - endPoint) * length;
                Player.velocity += (pullPoint - Player.Center) * 0.07f;
                extraVelocity = (pullPoint - Player.Center) * 0.05f;
            }
        }

		public override void DrawActiveEffects(SpriteBatch spriteBatch)
		{
            if (!Active)
                return;

            var tex = ModContent.GetTexture(AssetDirectory.Debug);

            var dist = Vector2.Distance(Player.Center, endPoint);

            for (int k = 0; k < dist; k += 10)
            {
                spriteBatch.Draw(tex, Vector2.Lerp(Player.Center, endPoint, k / (float)dist) - Main.screenPosition, null, Color.White, 0, tex.Size() / 2, 0.25f, 0, 0);
                Lighting.AddLight(Vector2.Lerp(Player.Center, endPoint, k  / (float)dist), new Vector3(1, 0.9f, 0.3f) * 0.5f);
            }

            Utils.DrawBorderString(spriteBatch, Player.velocity.Length() + " m/s", Player.Center + Vector2.UnitY * -40 - Main.screenPosition, Color.White);
		}

		public override void OnExit()
        {

        }

        public override bool HotKeyMatch(TriggersSet triggers, AbilityHotkeys abilityKeys)
        {
            return abilityKeys.Get<Whip>().Current;
        }
    }
}