﻿//TODO:
//Obtainment
//Sellprice
//Rarity
//Balance
//Fix collision issues
//Lighting
//Dust
using log4net.Core;
using ReLogic.Content;
using StarlightRiver.Helpers;
using System;
using System.Collections.Generic;
using Terraria.Audio;
using Terraria.GameContent.Creative;
using Terraria.ID;
using Terraria.Graphics.Effects;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TaskbarClock;

namespace StarlightRiver.Content.Items.Misc
{
	public class ThunderBeads : ModItem
	{
		public override string Texture => AssetDirectory.MiscItem + Name;

		public override void SetStaticDefaults()
		{
			DisplayName.SetDefault("Thunder Beads");
			Tooltip.SetDefault("Whip enemies to stick the beads in them \nRepeatedly click to shock affected enemies");
			CreativeItemSacrificesCatalog.Instance.SacrificeCountNeededByItemId[Type] = 1;
		}

		public override void SetDefaults()
		{
			Item.DefaultToWhip(ModContent.ProjectileType<ThunderBeads_Whip>(), 15, 1.2f, 5f, 25);
			Item.value = Item.sellPrice(0, 1, 0, 0);
			Item.rare = ItemRarityID.Green;
		}
	}

	public class ThunderBeads_Whip : BaseWhip
	{
		public NPC target = default;

		public bool embedded = false;

		public int embedTimer = 150;

		public bool ableToHit = false;
		public bool leftClick = true;

		public float fade = 0.1f;

		public Color baseColor = new(200, 230, 255);
		public Color endColor = Color.Purple;

		private Trail trail;
		private Trail trail2;
		private List<Vector2> cache;
		private List<Vector2> cache2;

		public override string Texture => AssetDirectory.MiscItem + Name;

		public ThunderBeads_Whip() : base("Thunder Beads", 15, 0.87f, Color.Transparent)
		{
			xFrames = 1;
			yFrames = 5;
		}

		public override int SegmentVariant(int segment)
		{
			return 1;
		}

		public override bool PreAI()
		{
			Projectile.ownerHitCheck = false;
			Projectile.localNPCHitCooldown = 1;

			if (!Main.dedServ)
			{
				ManageCache();
				ManageTrails();
			}

			if (embedded)
			{
				if (!leftClick && Main.mouseLeft)
				{
					ableToHit = true;
					leftClick = true;
				}

				if (!Main.mouseLeft)
					leftClick = false;

				Player player = Main.player[Projectile.owner];
				flyTime = player.itemAnimationMax * Projectile.MaxUpdates;
				Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
				Projectile.Center = Main.GetPlayerArmPosition(Projectile) + Projectile.velocity * (Projectile.ai[0] - 1f);
				Projectile.spriteDirection = (!(Vector2.Dot(Projectile.velocity, Vector2.UnitX) < 0f)) ? 1 : -1;

				player.heldProj = Projectile.whoAmI;
				player.itemAnimation = player.itemAnimationMax - (int)(Projectile.ai[0] / Projectile.MaxUpdates);
				player.itemTime = player.itemAnimation;

				embedTimer--;
				if (embedTimer < 0 || !target.active)
				{
					Projectile.friendly = false;
					embedded = false;
					return false;
				}

				if (fade > 0.1f)
					fade -= 0.05f;

				Projectile.WhipPointsForCollision.Clear();
				SetPoints(Projectile.WhipPointsForCollision);
				return false;
			}
			return base.PreAI();
		}

		public override void ArcAI()
		{
			xFrame = 0;
		}

		public override bool ShouldDrawSegment(int segment)
		{
			return segment % 3 == 0;
		}

		public override void OnHitNPC(NPC target, int damage, float knockback, bool crit)
		{
			ableToHit = false;
			if (!embedded)
			{
				Projectile.ownerHitCheck = false;
				this.target = target;
				embedded = true;
			}
			else
				fade = 1;
		}

		public override bool? CanHitNPC(NPC target)
		{
			if (embedded)
				return target == this.target && ableToHit;
			return base.CanHitNPC(target);
		}

		public override void SetPoints(List<Vector2> controlPoints)
		{
			if (embedded)
			{
				Player player = Main.player[Projectile.owner];
				Item heldItem = player.HeldItem;
				Vector2 playerArmPosition = Main.GetPlayerArmPosition(Projectile);
				for (int i = 0; i < segments + 1; i++)
				{
					float lerper = i / (float)segments;
					controlPoints.Add(Vector2.Lerp(playerArmPosition, target.Center, lerper));
				}
			}
			else
				base.SetPoints(controlPoints);
		}

		public override void DrawBehindWhip(ref Color lightColor)
		{
			DrawPrimitives();
			Texture2D bloomTex = ModContent.Request<Texture2D>(AssetDirectory.Keys + "Glow").Value;
			for (int i = 0; i < cache.Count - 1; i++)
			{
				if (i % 3 != 0)
					continue;

				Main.spriteBatch.Draw(bloomTex, cache[i] - Main.screenPosition, null, Color.White * 0.7f, 0, bloomTex.Size() / 2, fade * 0.4f, SpriteEffects.None, 0f);
			}

			Main.spriteBatch.End();
			Main.spriteBatch.Begin(default, default, default, default, default, default, Main.GameViewMatrix.TransformationMatrix);
		}

		private void ManageCache()
		{
			cache = new List<Vector2>();
			SetPoints(cache);

			cache2 = new List<Vector2>();
			for (int i = 0; i < cache.Count; i++)
			{
				Vector2 point = cache[i];
				Vector2 endPoint = embedded ? target.Center : cache[i];
				Vector2 nextPoint = i == cache.Count - 1 ? endPoint : cache[i + 1];
				Vector2 dir = Vector2.Normalize(nextPoint - point).RotatedBy(Main.rand.NextBool() ? -1.57f : 1.57f);
				if (i > cache.Count - 3 || dir == Vector2.Zero)
					cache2.Add(point);
				else
					cache2.Add(point + dir * Main.rand.NextFloat(8) * fade);
			}
		}

		private void ManageTrails()
		{
			Vector2 endPoint = embedded ? target.Center : cache[segments];
			trail ??= new Trail(Main.instance.GraphicsDevice, segments + 1, new TriangularTip(4), factor => 16, factor =>
			{
				if (factor.X > 0.99f)
					return Color.Transparent;

				return new Color(160, 220, 255) * fade * 0.1f * EaseFunction.EaseCubicOut.Ease(1 - factor.X);
			});

			trail.Positions = cache.ToArray();
			trail.NextPosition = endPoint;
			trail2 ??= new Trail(Main.instance.GraphicsDevice, segments + 1, new TriangularTip(4), factor => 3 * Main.rand.NextFloat(0.55f, 1.45f), factor =>
			{
				float progress = EaseFunction.EaseCubicOut.Ease(1 - factor.X);
				return Color.Lerp(baseColor, endColor, EaseFunction.EaseCubicIn.Ease(1 - progress)) * fade * progress;
			});

			trail2.Positions = cache2.ToArray();
			trail2.NextPosition = endPoint;
		}

		public void DrawPrimitives()
		{
			Main.spriteBatch.End();
			Effect effect = Filters.Scene["LightningTrail"].GetShader().Shader;

			var world = Matrix.CreateTranslation(-Main.screenPosition.Vec3());
			Matrix view = Main.GameViewMatrix.ZoomMatrix;
			var projection = Matrix.CreateOrthographicOffCenter(0, Main.screenWidth, Main.screenHeight, 0, -1, 1);

			effect.Parameters["time"].SetValue(Main.GameUpdateCount * 0.05f);
			effect.Parameters["repeats"].SetValue(1f);
			effect.Parameters["transformMatrix"].SetValue(world * view * projection);
			effect.Parameters["sampleTexture"].SetValue(ModContent.Request<Texture2D>("StarlightRiver/Assets/GlowTrail").Value);

			trail?.Render(effect);
			trail2?.Render(effect);

			Main.spriteBatch.Begin(default, BlendState.Additive, default, default, default, default, Main.GameViewMatrix.TransformationMatrix);
		}
	}
}