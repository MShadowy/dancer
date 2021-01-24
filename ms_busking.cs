using System;
using System.Collections.Generic;
using XRL.UI;
using XRL.Rules;
using XRL.Messages;

namespace XRL.World.Parts.Skill
{
    [Serializable]
    public class MS_Busking : BaseSkill
    {
        public Guid ActivatedAbilityID = Guid.Empty;

        public ActivatedAbilityEntry ActivatedAbility;

        public MS_Busking()
        {
            base.Name = "MS_Busking";
            DisplayName = "MS_Busking";
        }

        public override void Register(GameObject Object)
        {
            Object.RegisterPartEvent(this, "CommandCustomsBusking");
            base.Register(Object);
        }

        public int CalculateStatBonus()
        {
            int Bonus = 0;
            int AgiBonus = ParentObject.Statistics["Agility"].Bonus;
            int EgoBonus = ParentObject.Statistics["Ego"].Bonus;

            if (AgiBonus > 0 && EgoBonus > 0)
            {
                Bonus = (AgiBonus + EgoBonus) / 2;
            }
            else if (AgiBonus > EgoBonus)
            {
                Bonus = AgiBonus;
            }
            else
            {
                Bonus = EgoBonus;
            }

            return Bonus;
        }

        public void BuskingEffect(Cell cell, GameObject target)
        {
            int TBase = 14;
            if (cell != null)
            {
                target = cell.GetCombatTarget(ParentObject, true, false, false, null, false, false, false, null);
                Cell dropOff = ParentObject.CurrentCell.GetFirstEmptyAdjacentCell(1,1);
                if (target !=null)
                {
                    int DieRollA = Stat.Roll("1d20");
                    if (DieRollA == 20 || DieRollA + CalculateStatBonus() > 15 + target.Statistics["Willpower"].Bonus + target.Statistics["Level"].BaseValue)
                    {
                        int tipLevel = 0;
                        for (int i =0; i < 3; i++)
                        {
                            int DieRollB = Stat.Roll("3d6");
                            if (DieRollB > TBase + tipLevel)
                            {
                                tipLevel = tipLevel + 1;
                            }
                        }
                        if (tipLevel > 0)
                        {

                            int rand = Stat.Random(1, 100);
                            if (tipLevel == 3)
                            {
                                string BP;
                                int tier = target.GetTier();
                                if (rand <= 33)
                                {
                                    BP = "Armor " + tier.ToString();
                                }
                                else if (rand > 33 && rand <= 66)
                                {
                                    BP = "Missile Weapon " + tier.ToString();
                                }
                                else
                                {
                                    BP = "Melee Weapon " + tier.ToString();
                                }
                                dropOff.AddTableObject("Melee Weapon 1");

                                if (ParentObject.IsPlayer())
                                {
                                    MessageQueue.AddPlayerMessage("&g" + target.BaseDisplayName + " pauses for a while to watch you perform. " +
                                                "After a short while they stride away, dropping off a " + BP + " at your feet with a soft thunk as they leave. &y");
                                }
                            }
                            else if (tipLevel == 2)
                            {
                                string BP;
                                if (rand > 95)
                                {
                                    BP = "Silver Nugget";
                                }
                                else
                                {
                                    BP = "Copper Nugget";
                                }
                                dropOff.AddTableObject("Junk 1");

                                if (ParentObject.IsPlayer())
                                {
                                    MessageQueue.AddPlayerMessage("&gA dull metallic clunk rings out as a " + BP.ToLower() +
                                                " is dropped carelessly into your busking pan by a passing" + target.BaseDisplayName + "&y.");
                                }
                            }
                            else
                            {
                                int Drams = Stat.Roll("1d2");
                                dropOff.AddObject("FreshWaterTip", Drams);

                                if (ParentObject.IsPlayer())
                                {
                                    MessageQueue.AddPlayerMessage("&gYour busking pan hums as a dram or two of water is poured into it by "
                                                + target.BaseDisplayName + " as they hurry past. &y");
                                }
                            }
                        }
                        else if (tipLevel == 0)
                        {
                            int rand = Stat.Random(1, 100);
                            if (rand < 51)
                            {
                                if (ParentObject.IsPlayer())
                                {
                                    MessageQueue.AddPlayerMessage(target.BaseDisplayName + " glances your direction for a moment, before continuing on their way. &y");
                                }
                            }
                        }
                    }
                    else if (DieRollA + CalculateStatBonus() <= 15 + target.Statistics["Willpower"].Bonus + target.Statistics["Level"].BaseValue)
                    {
                        int rand = Stat.Random(1, 100);
                        if (rand < 26)
                        {
                            if (ParentObject.IsPlayer())
                            {
                                MessageQueue.AddPlayerMessage(target.BaseDisplayName + " brushes by, paying you no mind. &y");
                            }
                        }
                    }

                    CooldownMyActivatedAbility(ActivatedAbilityID, 500, null, "Agility");
                }
            }
        }

        public override bool FireEvent(Event E)
        {
            if (E.ID == "CommandCustomsBusking")
            {
                if (ParentObject.pPhysics != null && ParentObject.pPhysics.IsFrozen())
                {
                    if (ParentObject.IsPlayer())
                    {
                        Popup.Show("You are frozen solid!", true);
                    }
                    return true;
                }
                if (ParentObject.AreHostilesNearby())
                {
                    if (ParentObject.IsPlayer())
                    {
                        Popup.Show("You can't perform with enemies nearby!", true);
                    }
                    return true;
                }
                int rand = Stat.Roll("5d6");
                int dur = 120 + rand;
                int range = 6;
                int count = 0;
                Cell current = ParentObject.CurrentCell;
                List<GameObject> effected = new List<GameObject>();

                if (current != null)
                {
                    if (ParentObject.IsPlayer())
                    {
                        MessageQueue.AddPlayerMessage("&gYou lay down a tin nearby and begin your performance. &y");
                    }

                    foreach (GameObject viewer in current.ParentZone.FastSquareSearch(current.X, current.Y, range, "Combat"))
                    {
                        if (viewer == ParentObject) { } else
                        {
                            if (ParentObject.DistanceTo(viewer) <= range && viewer.HasPart("Brain") && !viewer.pBrain.IsHostileTowards(ParentObject)
                                    && !effected.Contains(viewer))
                            {
                                BuskingEffect(current, viewer);
                                effected.Add(viewer);
                            }
                        }
                    }

                    if (count > 1 && ParentObject.AreHostilesNearby())
                    {
                        count = dur;
                        if (ParentObject.IsPlayer())
                        {
                            Popup.Show("Your performance is interrupted! &y");
                        }
                    }
                    while (count < dur)
                    {
                        if (ParentObject.IsValid())
                        {
                            ParentObject.UseEnergy(1000, "Pass", null);
                            count = count + 1;
                        }
                    }
                    if (count == dur)
                    {
                        if (ParentObject.IsPlayer())
                        {
                            MessageQueue.AddPlayerMessage("&gYou wrap up your show. &y");
                        }
                    }
                }
            }

            return base.FireEvent(E);
        }

        public override bool AddSkill(GameObject GO)
        {
            string description = "Through dance, song, or some other method, you catch the eyes of passersby within a 6 tile radius and entertain them for a while, "
                        + "in hopes of gathering water or other valuables. This action is prolonged, lasting between 125 and 150 turns.";
            ActivatedAbilityID = AddMyActivatedAbility("Busking", "CommandCustomsBusking", "Skill", description, "*");
            return true;
        }

        public override bool RemoveSkill(GameObject GO)
        {
            if (ActivatedAbilityID != Guid.Empty)
            {
                ActivatedAbilities activatedAbilities = GO.GetPart("ActivatedAbilities") as ActivatedAbilities;
                activatedAbilities.RemoveAbility(ActivatedAbilityID);
            }
            return true;
        }
    }
}
