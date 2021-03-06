#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.AI
{
	abstract class StateBase
	{
		protected const int DangerRadius = 10;

		protected static void GoToRandomOwnBuilding(Squad squad)
		{
			var loc = RandomBuildingLocation(squad);
			foreach (var a in squad.Units)
				squad.World.IssueOrder(new Order("Move", a, false) { TargetLocation = loc });
		}

		protected static CPos RandomBuildingLocation(Squad squad)
		{
			var location = squad.Bot.BaseCenter;
			var buildings = squad.World.ActorsWithTrait<Building>()
				.Where(a => a.Actor.Owner == squad.Bot.Player).Select(a => a.Actor).ToList();
			if (buildings.Count > 0)
				location = buildings.Random(squad.Random).Location;
			return location;
		}

		protected static bool BusyAttack(Actor a)
		{
			if (a.IsIdle)
				return false;

			var type = a.GetCurrentActivity().GetType();
			if (type == typeof(Attack) || type == typeof(FlyAttack))
				return true;

			var next = a.GetCurrentActivity().NextActivity;
			if (next == null)
				return false;

			var nextType = a.GetCurrentActivity().NextActivity.GetType();
			if (nextType == typeof(Attack) || nextType == typeof(FlyAttack))
				return true;

			return false;
		}

		protected static bool CanAttackTarget(Actor a, Actor target)
		{
			if (!a.HasTrait<AttackBase>())
				return false;

			var targetable = target.TraitOrDefault<ITargetable>();
			if (targetable == null)
				return false;

			var arms = a.TraitsImplementing<Armament>();
			foreach (var arm in arms)
				if (arm.Weapon.ValidTargets.Intersect(targetable.TargetTypes).Any())
					return true;

			return false;
		}

		protected virtual bool ShouldFlee(Squad squad, Func<IEnumerable<Actor>, bool> flee)
		{
			if (!squad.IsValid)
				return false;

			var u = squad.Units.Random(squad.Random);
			var units = squad.World.FindActorsInCircle(u.CenterPosition, WRange.FromCells(DangerRadius)).ToList();
			var ownBaseBuildingAround = units.Where(unit => unit.Owner == squad.Bot.Player && unit.HasTrait<Building>());
			if (ownBaseBuildingAround.Any())
				return false;

			var enemyAroundUnit = units.Where(unit => squad.Bot.Player.Stances[unit.Owner] == Stance.Enemy && unit.HasTrait<AttackBase>());
			if (!enemyAroundUnit.Any())
				return false;

			return flee(enemyAroundUnit);
		}
	}
}
