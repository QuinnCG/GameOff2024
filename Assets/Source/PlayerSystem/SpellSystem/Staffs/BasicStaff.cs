﻿using FMODUnity;
using Quinn.MissileSystem;
using Sirenix.OdinInspector;
using Unity.VisualScripting;
using UnityEngine;

namespace Quinn.PlayerSystem.SpellSystem.Staffs
{
	public class BasicStaff : Staff
	{
		[SerializeField, FoldoutGroup("SFX")]
		private EventReference BasicCastSound;
		[SerializeField, ShowIf(nameof(HasBasicFinisher)), FoldoutGroup("SFX")]
		private EventReference BasicFinisherCastSound;
		[SerializeField, ShowIf(nameof(HasSpecial)), FoldoutGroup("SFX")]
		private EventReference SpecialCastLittleSound, SpecialCastBigSound, FullChargeSound;

		[SerializeField, Required, FoldoutGroup("Basic")]
		private Missile BasicMissile;
		[SerializeField, FoldoutGroup("Basic"), Unit(Units.Second)]
		private float BasicCooldown = 0.3f;
		[SerializeField, FoldoutGroup("Basic"), Unit(Units.MetersPerSecond)]
		private float BasicKnockbackSpeed = 10f;
		[SerializeField, FoldoutGroup("Basic")]
		private MissileSpawnBehavior BasicBehavior = MissileSpawnBehavior.Direct;
		[SerializeField, HideIf("@BasicBehavior == MissileSpawnBehavior.Direct"), FoldoutGroup("Basic"), Unit(Units.Degree)]
		private float BasicSpread = 0f;
		[SerializeField, FoldoutGroup("Basic")]
		private int BasicCount = 1;
		[SerializeField, ShowIf("@BasicCount > 1"), FoldoutGroup("Basic"), Unit(Units.Second)]
		private float BasicInterval = 0f;
		[SerializeField, ShowIf(nameof(HasBasicFinisher)), FoldoutGroup("Basic"), Unit(Units.Second)]
		private float ChainWindowDuration = 0.4f;
		[SerializeField, FoldoutGroup("Basic")]
		private float BasicEnergyUse = 2f;

		[Space, SerializeField, FoldoutGroup("Basic Finisher")]
		private bool HasBasicFinisher = true;
		[SerializeField, FoldoutGroup("Basic Finisher"), Unit(Units.Second)]
		private float BasicFinisherCooldown = 0.6f;
		[SerializeField, ShowIf(nameof(HasBasicFinisher)), FoldoutGroup("Basic Finisher")]
		private int BasicFinisherCount = 3;
		[SerializeField, ShowIf(nameof(HasBasicFinisher)), FoldoutGroup("Basic Finisher")]
		private MissileSpawnBehavior BasicFinisherBehavior = MissileSpawnBehavior.SpreadRandom;
		[SerializeField, HideIf("@BasicFinisherBehavior == MissileSpawnBehavior.Direct || HasBasicFinisher"), FoldoutGroup("Basic Finisher"), Unit(Units.Degree)]
		private float BasicFinisherSpread = 45f;
		[SerializeField, ShowIf(nameof(HasBasicFinisher)), FoldoutGroup("Basic Finisher"), Unit(Units.MetersPerSecond)]
		private float BasicFinisherKnockbackSpeed = 14f;
		[SerializeField, ShowIf(nameof(HasBasicFinisher)), FoldoutGroup("Basic Finisher")]
		[Tooltip("This can be null to use the basic normal missile.")]
		private Missile BasicFinisherMissileOverride;
		[SerializeField, FoldoutGroup("Basic Finisher")]
		private float BasicFinisherEnergyUse = 4f;

		[Space, SerializeField, FoldoutGroup("Special")]
		private bool HasSpecial = true;
		[SerializeField, Required, FoldoutGroup("Special"), ShowIf(nameof(HasSpecial))]
		private Missile SpecialMissile;
		[SerializeField, FoldoutGroup("Special"), ShowIf(nameof(HasSpecial)), Unit(Units.Second)]
		private float ChargingSparkInterval = 0.45f;
		[SerializeField, ShowIf(nameof(HasSpecial)), FoldoutGroup("Special"), Unit(Units.Second)]
		private float SpecialCooldown = 1f;
		[SerializeField, ShowIf(nameof(HasSpecial)), FoldoutGroup("Special"), Unit(Units.Second)]
		private float SpecialChargeTime = 1f;
		[SerializeField, ShowIf(nameof(HasSpecial)), FoldoutGroup("Special"), Unit(Units.MetersPerSecond)]
		private float SpecialKnockbackSpeed = 10f;
		[SerializeField, ShowIf(nameof(HasSpecial)), FoldoutGroup("Special")]
		private float ChargingMoveSpeedFactor = 0.5f;
		[SerializeField, ShowIf(nameof(HasSpecial)), FoldoutGroup("Special")]
		private int SpecialCount = 1;
		[SerializeField, ShowIf("@SpecialCount > 1 && HasSpecial"), FoldoutGroup("Special"), Unit(Units.Second)]
		private float SpecialInterval = 0f;
		[SerializeField, ShowIf(nameof(HasSpecial)), FoldoutGroup("Special")]
		private MissileSpawnBehavior SpecialBehavior = MissileSpawnBehavior.Direct;
		[SerializeField, HideIf("@SpecialBehavior == MissileSpawnBehavior.Direct || !HasSpecial"), FoldoutGroup("Special"), Unit(Units.Degree)]
		private float SpecialSpread = 0f;
		[SerializeField, FoldoutGroup("Special")]
		private float SpecialEnergyUse = 8f;

		private float _largeMissileTime;
		private int _castChainCount;
		private float _chainTimeoutTime;
		private bool _isMovePenaltyApplied;

		private void Update()
		{
			if (IsBasicHeld && CanCast)
			{
				OnBasicDown();
			}
		}

		private void FixedUpdate()
		{
			if (Caster == null)
				return;

			if (_castChainCount < BasicFinisherCount && _castChainCount > 0 && Time.time > _chainTimeoutTime)
			{
				_castChainCount = 0;
				Caster.SetCooldown(BasicCooldown);
			}

			if (_isMovePenaltyApplied && Time.time > _largeMissileTime && HasSpecial && IsSpecialHeld)
			{
				_isMovePenaltyApplied = false;
				Caster.Movement.RemoveSpeedModifier(this);

				Audio.Play(FullChargeSound);
				Caster.Movement.CanDash = true;
			}

			if (IsSpecialHeld && CanCast && HasSpecial)
			{
				Cooldown.Call(this, ChargingSparkInterval, Caster.Spark);
			}
		}

		public override void OnBasicDown()
		{
			if (!CanCast)
				return;

			_castChainCount++;
			Caster.Spark();

			// Finisher cast.
			if (_castChainCount >= BasicFinisherCount && HasBasicFinisher)
			{
				Caster.SetCooldown(BasicFinisherCooldown);
				_castChainCount = 0;

				var missile = BasicFinisherMissileOverride != null ? BasicFinisherMissileOverride : BasicMissile;

				MissileManager.Instance.SpawnMissile(Caster.gameObject, missile, Head.position, GetDirToCrosshair(),
					BasicFinisherCount, BasicFinisherBehavior, BasicFinisherSpread);
				Caster.Movement.Knockback(-GetDirToCrosshair(), BasicFinisherKnockbackSpeed);

				Audio.Play(BasicFinisherCastSound, Head.position);
				ConsumeEnergy(BasicFinisherEnergyUse);
			}
			// Normal cast.
			else
			{
				Caster.SetCooldown(BasicCooldown);
				MissileManager.Instance.SpawnMissile(Caster.gameObject, BasicMissile, Head.position, GetDirToCrosshair(),
					BasicCount, BasicInterval, BasicBehavior, BasicSpread);

				_chainTimeoutTime = Time.time + ChainWindowDuration + BasicCooldown;
				Caster.Movement.Knockback(-GetDirToCrosshair(), BasicKnockbackSpeed);

				Audio.Play(BasicCastSound, Head.position);
				ConsumeEnergy(BasicEnergyUse);
			}
		}

		public override void OnSpecialDown()
		{
			if (!HasSpecial || !CanCast)
				return;

			Caster.Movement.CanDash = false;

			Caster.SetCooldown(SpecialCooldown);
			_largeMissileTime = Time.time + SpecialChargeTime;

			Caster.Movement.ApplySpeedModifier(this, ChargingMoveSpeedFactor);
			_isMovePenaltyApplied = true;
			_castChainCount = 0;
		}

		public override void OnSpecialUp()
		{
			if (!HasSpecial)
				return;

			Caster.Spark();

			bool enoughCharge = Time.time > _largeMissileTime;

			var prefab = enoughCharge ? SpecialMissile : BasicMissile;
			MissileManager.Instance.SpawnMissile(Caster.gameObject, prefab, Head.position, GetDirToCrosshair(),
				SpecialCount, SpecialInterval, SpecialBehavior, SpecialSpread);

			Caster.Movement.Knockback(-GetDirToCrosshair(), SpecialKnockbackSpeed);
			Audio.Play(Time.time > _largeMissileTime ? SpecialCastBigSound : SpecialCastLittleSound, Head.position);

			Caster.Movement.RemoveSpeedModifier(this);

			if (enoughCharge)
			{
				ConsumeEnergy(SpecialEnergyUse);
			}
			else
			{
				ConsumeEnergy(BasicFinisherEnergyUse);
			}
		}

		private Vector2 GetDirToCrosshair()
		{
			return CrosshairManager.Instance.DirectionToCrosshair(Head.position);
		}
	}
}
