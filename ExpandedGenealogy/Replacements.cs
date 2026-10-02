using System;
using System.Collections.Generic;
using Sims3.Gameplay;
using Sims3.Gameplay.CAS;
using Sims3.Gameplay.EventSystem;
using Sims3.Gameplay.Socializing;
using Sims3.Gameplay.Utilities;
using Sims3.SimIFace;
using Sims3.SimIFace.CustomContent;
using Sims3.UI.CAS;
using Sims3.UI.Controller;
using Tuning = Sims3.Gameplay.Destrospean.ExpandedGenealogy;

namespace Destrospean.ExpandedGenealogy
{
    public class Replacements
    {
        public void AddChild(IGenealogy iChild)
        {
            Genealogy other = (Genealogy)iChild,
            self = (Genealogy)(object)this;
            if (other.mNaturalParents.Count == 2)
            {
                return;
            }
            List<Genealogy> siblings = new List<Genealogy>();
            if (other.mNaturalParents.Count == 0)
            {
                siblings.AddRange(other.Siblings);
            }
            siblings.Add(other);
            foreach (Genealogy sibling in siblings)
            {
                if (self.mChildren.Contains(sibling))
                {
                    continue;
                }
                self.ClearDerivedData();
                sibling.ClearDerivedData();
                foreach (Genealogy child in self.mChildren)
                {
                    child.ClearDerivedData();
                }
                List<Genealogy> tempAncestors = new List<Genealogy>();
                tempAncestors.AddRange(self.mNaturalParents);
                while (tempAncestors.Count > 0)
                {
                    Genealogy tempAncestor = tempAncestors[0];
                    tempAncestors.RemoveAt(0);
                    tempAncestor.ClearDerivedData();
                    tempAncestors.AddRange(tempAncestor.mNaturalParents);
                }
                self.mChildren.Add(sibling);
                sibling.mNaturalParents.Add(self);
                if (sibling.mSim != null && !sibling.IMiniSimDescription.IsEP11Bot && self.mSim != null && self.mSim.CreatedSim != null)
                {
                    EventTracker.SendEvent(new GotChildAndAgeTransitionEvent(self.mSim.CreatedSim, sibling.mSim.CreatedSim, false));
                    EventTracker.SendEvent(EventTypeId.kChildBornOrAdopted, null, sibling.mSim.CreatedSim);
                }
            }
            GenealogyExtended.RebuildRelationAssignments();
        }

        public void ClearDerivedData()
        {
            List<Genealogy> descendants = new List<Genealogy>
                {
                    (Genealogy)(object)this
                };
            while (descendants.Count > 0)
            {
                Genealogy descendant = descendants[0];
                descendants.RemoveAt(0);
                descendant.mAncestors = null;
                if (descendant.mSiblings != null && descendant.mNaturalParents.Count > 0)
                {
                    descendant.mSiblings = null;
                }
                if (descendant.mChildren != null)
                {
                    descendants.AddRange(descendant.mChildren);
                }
            }
            GenealogyPlaceholder.ClearCaches();
        }

        public string GetLTRRelationshipString(IMiniSimDescription sim1, IMiniSimDescription sim2)
        {
            string result = "";
            SimDescription simDescription1 = sim1 as SimDescription,
            simDescription2 = sim2 as SimDescription;
            if (simDescription1 == null || simDescription2 == null)
            {
                MiniSimDescription miniSimDescription1 = MiniSimDescription.Find(sim1.SimDescriptionId);
                if (miniSimDescription1 != null)
                {
                    foreach (MiniRelationship miniRelationship in miniSimDescription1.MiniRelationships)
                    {
                        if (miniRelationship.SimDescriptionId == sim2.SimDescriptionId)
                        {
                            return string.IsNullOrEmpty(miniRelationship.FamilialString) ? LTRData.Get(miniRelationship.CurrentLTR).GetName(sim1, sim2) : miniRelationship.FamilialString;
                        }
                    }
                    if (miniSimDescription1.Genealogy != null)
                    {
                        if (simDescription2 != null && simDescription2.Genealogy != null)
                        {
                            return miniSimDescription1.Genealogy.GetMyFamilialDescriptionFor(simDescription2.Genealogy);
                        }
                        MiniSimDescription miniSimDescription2 = sim2 as MiniSimDescription;
                        if (miniSimDescription2 != null && miniSimDescription2.Genealogy != null)
                        {
                            return miniSimDescription1.Genealogy.GetMyFamilialDescriptionFor(miniSimDescription2.Genealogy);
                        }
                    }
                }
            }
            else
            {
                Relationship relationship = simDescription1.IsValidDescription && simDescription2.IsValidDescription ? Relationship.Get(simDescription1, simDescription2, false) : null;
                result = relationship == null ? simDescription1.GetMyFamilialDescriptionFor(simDescription2) : LTRData.Get(relationship.LTR.CurrentLTR).GetRelationshipText(simDescription1, simDescription2);
            }
            return result;
        }

        public string GetMyFamilialDescriptionFor(SimDescription other)
        {
            SimDescription self = (SimDescription)(object)this;
            if (other.Genealogy == self.Genealogy)
            {
                return "";
            }
            if (GameUtils.IsAnyTravelBasedWorld() && GameStates.TravelerIds != null && GameStates.TravelerIds.Contains(self.SimDescriptionId))
            {
                MiniSimDescription miniSimDescription = MiniSimDescription.Find(self.SimDescriptionId);
                if (miniSimDescription != null && miniSimDescription.MiniRelationships != null)
                {
                    foreach (MiniRelationship miniRelationship in miniSimDescription.MiniRelationships)
                    {
                        if (miniRelationship.SimDescriptionId == other.SimDescriptionId)
                        {
                            return miniRelationship.FamilialString;
                        }
                    }
                }
            }
            return self.Genealogy.GetMyFamilialDescriptionFor(other.Genealogy);
        }

        public bool IsBloodRelated(Genealogy other)
        {
            bool isSufficientlyRelatedToRuleOutRomance;
            ((Genealogy)(object)this).GetCoefficientOfRelationship(other, out isSufficientlyRelatedToRuleOutRomance, true);
            return isSufficientlyRelatedToRuleOutRomance;
        }

        /// <summary>Replacement method for NRaas Woohooer's `IsCloselyRelated` method</summary>
        /// <param name="thoroughCheck">Parameter made obsolete by this mod but is still required to replace the original method</param>
        public static bool IsCloselyRelated(SimDescription sim1, SimDescription sim2, bool thoroughCheck)
        {
            if (sim1 == sim2)
            {
                return true;
            }
            if (sim1 == null || sim2 == null || !(sim1.Species == sim2.Species || sim1.IsADogSpecies && sim2.IsADogSpecies) || sim1.IsRobot || sim2.IsRobot)
            {
                return false;
            }
            if ((sim1.Genealogy.IsFutureAncestorOf(sim2.Genealogy) || sim2.Genealogy.IsFutureAncestorOf(sim1.Genealogy)) && Tuning.kDenyRomanceWithAncestors)
            {
                return true;
            }
            return sim1.Genealogy.IsBloodRelated(sim2.Genealogy) || sim1.Genealogy.IsStepRelated(sim2.Genealogy);
        }

        public static bool IsCousin(Genealogy sim1, Genealogy sim2)
        {
            return GenealogyExtended.IsCousin(sim1, sim2);
        }

        public bool IsFutureBloodRelated(Genealogy other)
        {
            Genealogy self = (Genealogy)(object)this;
            if (other.SimDescription == null)
            {
                return false;
            }
            if ((self.IsFutureAncestorOf(other) || other.IsFutureAncestorOf(self)) && Tuning.kDenyRomanceWithAncestors)
            {
                return true;
            }
            return false;
        }

        public static bool IsGrandparent(Genealogy grandparent, Genealogy grandchild)
        {
            AncestorInfo ancestorInfo = grandchild.GetAncestorInfo(grandparent);
            return ancestorInfo != null && ancestorInfo.GenerationalDistance == 1;
        }

        public static bool IsGreatGrandparent(Genealogy greatGrandparent, Genealogy greatGrandchild)
        {
            AncestorInfo ancestorInfo = greatGrandchild.GetAncestorInfo(greatGrandparent);
            return ancestorInfo != null && ancestorInfo.GenerationalDistance == 2;
        }

        public static bool IsHalfSibling(Genealogy sim1, Genealogy sim2)
        {
            if (sim1 == null || sim2 == null)
            {
                return false;
            }
            int sharedParentCount = 0;
            foreach (Genealogy parent1 in sim1.Parents)
            {
                foreach (Genealogy parent2 in sim2.Parents)
                {
                    if (parent1 == parent2)
                    {
                        sharedParentCount++;
                    }
                }
            }
            return Genealogy.IsSibling(sim1, sim2) && sharedParentCount * 2 != sim1.Parents.Count + sim2.Parents.Count;
        }

        public static bool IsSiblingInLaw(Genealogy sim1, Genealogy sim2)
        {
            if (sim1.Spouse != null && sim1.PartnerType == PartnerType.Marriage)
            {
                foreach (Genealogy sibling in sim1.Spouse.Siblings)
                {
                    if (sibling == sim2 || sibling.Spouse == sim2 && sibling.PartnerType == PartnerType.Marriage)
                    {
                        return true;
                    }
                }
            }
            if (sim2.Spouse != null && sim2.PartnerType == PartnerType.Marriage)
            {
                foreach (Genealogy sibling in sim2.Spouse.Siblings)
                {
                    if (sibling == sim1 || sibling.Spouse == sim1 && sibling.PartnerType == PartnerType.Marriage)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public bool IsStepRelated(Genealogy other)
        {
            Genealogy self = (Genealogy)(object)this;
            foreach (Genealogy parent1 in self.Parents)
            {
                foreach (Genealogy parent2 in other.Parents)
                {
                    if (parent1.Spouse == parent2 && parent1.PartnerType == PartnerType.Marriage && Tuning.kDenyRomanceWithStepSiblings)
                    {
                        return true;
                    }
                }
            }
            foreach (Genealogy parent in self.Parents)
            {
                if (parent.Spouse == other && parent.PartnerType == PartnerType.Marriage && Tuning.kDenyRomanceWithStepParents)
                {
                    return true;
                }
            }
            foreach (Genealogy parent in other.Parents)
            {
                if (parent.Spouse == self && parent.PartnerType == PartnerType.Marriage && Tuning.kDenyRomanceWithStepParents)
                {
                    return true;
                }
            }
            return false;
        }

        public static bool IsUncle(Genealogy uncle, Genealogy nephew)
        {
            SiblingOfAncestorInfo siblingOfAncestorInfo = nephew.GetSiblingOfAncestorInfo(uncle);
            if (siblingOfAncestorInfo != null && siblingOfAncestorInfo.GenerationalDistance == 0)
            {
                return true;
            }
            if (uncle.Spouse == null || uncle.PartnerType != PartnerType.Marriage)
            {
                return false;
            }
            siblingOfAncestorInfo = nephew.GetSiblingOfAncestorInfo(uncle.Spouse);
            return siblingOfAncestorInfo != null && siblingOfAncestorInfo.GenerationalDistance == 0;
        }

        public ulong MakeUniqueId()
        {
            SimDescription self = (SimDescription)(object)this;
            ulong simDescriptionId = self.mSimDescriptionId;
            while (!self.IsSimDescriptionIdUnique(simDescriptionId) || GenealogyPlaceholder.GenealogyPlaceholders.ContainsKey(simDescriptionId))
            {
                simDescriptionId = DownloadContent.GenerateGUID();
            }
            if (simDescriptionId != self.mSimDescriptionId)
            {
                self.mOldSimDescriptionId = self.mSimDescriptionId;
            }
            self.mSimDescriptionId = simDescriptionId;
            if (self.CelebrityManager != null)
            {
                self.CelebrityManager.ResetOwnerSimDescription(self.mSimDescriptionId);
            }
            if (self.PetManager != null)
            {
                self.PetManager.ResetOwnerSimDescription(self.mSimDescriptionId);
            }
            if (self.TraitChipManager != null)
            {
                self.TraitChipManager.ResetOwnerSimDescription(self.mSimDescriptionId);
            }
            return simDescriptionId;
        }
    }
}
