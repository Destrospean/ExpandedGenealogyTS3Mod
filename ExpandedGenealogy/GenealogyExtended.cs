using System;
using System.Collections.Generic;
using Sims3.Gameplay.CAS;
using Sims3.Gameplay.Socializing;
using Sims3.Gameplay.Utilities;
using Sims3.UI.CAS;
using Sims3.UI.Controller;
using Tuning = Sims3.Gameplay.Destrospean.ExpandedGenealogy;

namespace Destrospean.ExpandedGenealogy
{
    public static class GenealogyExtended
    {
        static List<Dictionary<string, object>> RelationAssignments
        {
            get
            {
                return (List<Dictionary<string, object>>)typeof(Common).GetField("sRelationAssignments", (System.Reflection.BindingFlags)0x28).GetValue(null);
            }
        }

        static class RelationAssignmentFieldNames
        {
            public const string Degree = "Degree",
            GenerationalDistance = "Generational Distance",
            IsHigherUpFamilyTree = "Sim A Is Higher Up Family Tree",
            RelationType = "Relation Type",
            SimA = "Sim A",
            SimB = "Sim B";
        }

        static class RelationTypeNames
        {
            public const string Cousin = "Cousin",
            Descendant = "Descendant",
            DescendantOfSibling = "Descendant Of Sibling";
        }

        static bool TryAddDistantRelationInfo(this List<DistantRelationInfo> distantRelationInfoList, int degree, int timesRemoved, GenealogyPlaceholder throughWhichChild1, GenealogyPlaceholder throughWhichChild2, GenealogyPlaceholder closestDescendant)
        {
            DistantRelationInfo distantRelationInfo = new DistantRelationInfo(degree, timesRemoved, closestDescendant, new[]
                {
                    throughWhichChild1,
                    throughWhichChild2
                });
            if (!distantRelationInfoList.Exists(tempDistantRelationInfo => Array.Exists(tempDistantRelationInfo.ThroughWhichChildren, x => !Array.Exists(distantRelationInfo.ThroughWhichChildren, x.Equals)) && tempDistantRelationInfo.ClosestDescendant == distantRelationInfo.ClosestDescendant && tempDistantRelationInfo.Degree == distantRelationInfo.Degree && tempDistantRelationInfo.TimesRemoved == distantRelationInfo.TimesRemoved) && distantRelationInfo.Degree <= (uint)Tuning.kMaxDegreeCousinsToShow && distantRelationInfo.TimesRemoved <= (uint)Tuning.kMaxTimesRemovedCousinsToShow)
            {
                distantRelationInfoList.Add(distantRelationInfo);
                return true;
            }
            return false;
        }

        internal static float GetCoefficientOfRelationship(this Genealogy self, Genealogy other, out bool isSufficientlyRelatedToRuleOutRomance, bool returnEarly = false)
        {
            isSufficientlyRelatedToRuleOutRomance = false;
            double relationshipCoefficient = 0;
            // Check if the target is an ancestor of the selected Sim.
            foreach (AncestorInfo ancestorInfo in self.GetAncestorInfoList(other))
            {
                if ((isSufficientlyRelatedToRuleOutRomance = isSufficientlyRelatedToRuleOutRomance || Tuning.kDenyRomanceWithAncestors) && returnEarly)
                {
                    return (float)relationshipCoefficient;
                }
                relationshipCoefficient += Math.Pow(2, -ancestorInfo.GenerationalDistance - 1);
            }
            // Check if the selected Sim is an ancestor of the target.
            foreach (AncestorInfo ancestorInfo in other.GetAncestorInfoList(self))
            {
                if ((isSufficientlyRelatedToRuleOutRomance = isSufficientlyRelatedToRuleOutRomance || Tuning.kDenyRomanceWithAncestors) && returnEarly)
                {
                    return (float)relationshipCoefficient;
                }
                relationshipCoefficient += Math.Pow(2, -ancestorInfo.GenerationalDistance - 1);
            }
            // Check if the Sims are siblings.
            if (Genealogy.IsSibling(self, other))
            {
                bool isHalfSibling = Genealogy.IsHalfSibling(self, other);
                if ((isSufficientlyRelatedToRuleOutRomance = isSufficientlyRelatedToRuleOutRomance || Tuning.kDenyRomanceWithSiblings && !(isHalfSibling && Tuning.kAllowRomanceForHalfRelatives)) && returnEarly)
                {
                    return (float)relationshipCoefficient;
                }
                relationshipCoefficient += isHalfSibling ? .25 : .5;
            }
            // Check if the target is a sibling of one of the selected Sim's ancestors.
            foreach (SiblingOfAncestorInfo siblingOfAncestorInfo in self.GetSiblingOfAncestorInfoList(other))
            {
                if ((isSufficientlyRelatedToRuleOutRomance = isSufficientlyRelatedToRuleOutRomance || Tuning.kDenyRomanceWithSiblingsOfAncestors && !(siblingOfAncestorInfo.IsHalfRelative && Tuning.kAllowRomanceForHalfRelatives)) && returnEarly)
                {
                    return (float)relationshipCoefficient;
                }
                relationshipCoefficient += Math.Pow(2, -siblingOfAncestorInfo.GenerationalDistance - (siblingOfAncestorInfo.IsHalfRelative ? 3 : 2));
            }
            // Check if the selected Sim is a sibling of one of the target's ancestors.
            foreach (SiblingOfAncestorInfo siblingOfAncestorInfo in other.GetSiblingOfAncestorInfoList(self))
            {
                if ((isSufficientlyRelatedToRuleOutRomance = isSufficientlyRelatedToRuleOutRomance || Tuning.kDenyRomanceWithSiblingsOfAncestors && !(siblingOfAncestorInfo.IsHalfRelative && Tuning.kAllowRomanceForHalfRelatives)) && returnEarly)
                {
                    return (float)relationshipCoefficient;
                }
                relationshipCoefficient += Math.Pow(2, -siblingOfAncestorInfo.GenerationalDistance - (siblingOfAncestorInfo.IsHalfRelative ? 3 : 2));
            }
            foreach (DistantRelationInfo distantRelationInfo in other.GetDistantRelationInfoList(self))
            {
                /* Check if the Sims are too closely related for romantic interactions depending on whether their degree of cousinage
                 * and the generational distance between them are below the minimums that determine that they are not, and if so, then check whether they are half-relatives,
                 * the latter of which matters depending on whether romantic interactions between distant half-relatives are allowed.
                 */
                if ((isSufficientlyRelatedToRuleOutRomance = isSufficientlyRelatedToRuleOutRomance || distantRelationInfo.Degree < (uint)Tuning.kMinDegreeCousinsToAllowRomance && distantRelationInfo.TimesRemoved < (uint)Tuning.kMinTimesRemovedCousinsToAllowRomance && !(distantRelationInfo.IsHalfRelative && Tuning.kAllowRomanceForHalfRelatives)) && returnEarly)
                {
                    return (float)relationshipCoefficient;
                }
                relationshipCoefficient += Math.Pow(2, -2 * distantRelationInfo.Degree - distantRelationInfo.TimesRemoved - (distantRelationInfo.IsHalfRelative ? 2 : 1));
            }
            /* Check if the coefficient of relationship for the two Sims is higher than the minimum to disallow romance.
             * If the minimum value is less than 0, then the coefficient of relationship does not determine whether romance between two Sims is allowed.
             */
            isSufficientlyRelatedToRuleOutRomance = isSufficientlyRelatedToRuleOutRomance || relationshipCoefficient >= Tuning.kMinRelationshipCoefficientToDenyRomance && Tuning.kMinRelationshipCoefficientToDenyRomance >= 0;
            return (float)relationshipCoefficient;
        }

        /// <summary>Assigns an ancestor to a Sim without knowing the Sims between the Sim and said ancestor.</summary>
        /// <param name="descendant">The descendant within the ancestor–descendant relationship</param>
        /// <param name="ancestor">The ancestor within the ancestor–descendant relationship</param>
        /// <param name="generationalDistance">The generational distance between the ancestor and descendant, with 0 for parents and children, 1 for grandparents and grandchildren, etc.</param>
        /// <param name="addRelationAssignment">Determine whether to save the assignment as an instruction for rebuilding the relations later</param>
        public static void AddAncestor(this Genealogy descendant, Genealogy ancestor, int generationalDistance = 1, bool addRelationAssignment = true)
        {
            if (addRelationAssignment)
            {
                RelationAssignments.Add(new Dictionary<string, object>
                    {
                        {
                            RelationAssignmentFieldNames.RelationType,
                            RelationTypeNames.Descendant
                        },
                        {
                            RelationAssignmentFieldNames.GenerationalDistance,
                            generationalDistance
                        },
                        {
                            RelationAssignmentFieldNames.SimA,
                            descendant
                        },
                        {
                            RelationAssignmentFieldNames.SimB,
                            ancestor
                        }
                    });
            }
            List<GenealogyPlaceholder> ancestry = new List<GenealogyPlaceholder>
                {
                    descendant.GetGenealogyPlaceholder()
                };
            for (int i = 0; i < generationalDistance; i++)
            {
                GenealogyPlaceholder fakeAncestor = new GenealogyPlaceholder();
                GenealogyPlaceholder.GenealogyPlaceholders.Add(fakeAncestor.Id, fakeAncestor);
                ancestry.Add(fakeAncestor);
            }
            ancestry.Add(ancestor.GetGenealogyPlaceholder());
            for (int i = 0; i < ancestry.Count - 1; i++)
            {
                ancestry[i].AddParent(ancestry[i + 1]);
            }
        }

        /// <summary>Assigns a cousin to a Sim without knowing the Sims that bridge the Sim and said cousin.</summary>
        /// <param name="self">The Sim to be given a relative</param>
        /// <param name="other">The Sim to be assigned as a relative</param>
        /// <param name="degree">The degree of cousinage between the two Sims</param>
        /// <param name="timesRemoved">The number of removals of generations between the two Sims</param>
        /// <param name="isHigherUpFamilyTree">Determine whether the Sim to be given a relative is higher up in the family tree</param>
        /// <param name="addRelationAssignment">Determine whether to save the assignment as an instruction for rebuilding the relations later</param>
        public static void AddCousin(this Genealogy self, Genealogy other, int degree = 1, int timesRemoved = 0, bool isHigherUpFamilyTree = true, bool addRelationAssignment = true)
        {
            if (addRelationAssignment)
            {
                RelationAssignments.Add(new Dictionary<string, object>
                    {
                        {
                            RelationAssignmentFieldNames.RelationType,
                            RelationTypeNames.Cousin
                        },
                        {
                            RelationAssignmentFieldNames.GenerationalDistance,
                            timesRemoved
                        },
                        {
                            RelationAssignmentFieldNames.Degree,
                            degree
                        },
                        {
                            RelationAssignmentFieldNames.SimA,
                            self
                        },
                        {
                            RelationAssignmentFieldNames.SimB,
                            other
                        },
                        {
                            RelationAssignmentFieldNames.IsHigherUpFamilyTree,
                            isHigherUpFamilyTree
                        }
                    });
            }
            List<GenealogyPlaceholder>[] ancestries = new[]
                {
                    new List<GenealogyPlaceholder>
                    {
                        self.GetGenealogyPlaceholder()
                    },
                    new List<GenealogyPlaceholder>
                    {
                        other.GetGenealogyPlaceholder()
                    }
                };
            GenealogyPlaceholder sharedFakeAncestor = new GenealogyPlaceholder();
            GenealogyPlaceholder.GenealogyPlaceholders.Add(sharedFakeAncestor.Id, sharedFakeAncestor);
            for (int i = 0; i < 2; i++)
            {
                for (int j = 0; j < degree + (isHigherUpFamilyTree ^ i == 0 ? timesRemoved : 0); j++)
                {
                    GenealogyPlaceholder fakeAncestor = new GenealogyPlaceholder();
                    GenealogyPlaceholder.GenealogyPlaceholders.Add(fakeAncestor.Id, fakeAncestor);
                    ancestries[i].Add(fakeAncestor);
                }
                ancestries[i][ancestries[i].Count - 1].AddParent(sharedFakeAncestor);
                for (int j = 0; j < ancestries[i].Count - 1; j++)
                {
                    ancestries[i][j].AddParent(ancestries[i][j + 1]);
                }
            }
        }

        /// <summary>Assigns a descendant to a Sim without knowing the Sims between the Sim and said descendant.</summary>
        /// <param name="ancestor">The ancestor within the ancestor–descendant relationship</param>
        /// <param name="descendant">The descendant within the ancestor–descendant relationship</param>
        /// <param name="generationalDistance">The generational distance between the ancestor and descendant, with 0 for parents and children, 1 for grandparents and grandchildren, etc.</param>
        /// <param name="addRelationAssignment">Determine whether to save the assignment as an instruction for rebuilding the relations later</param>
        public static void AddDescendant(this Genealogy ancestor, Genealogy descendant, int generationalDistance = 1, bool addRelationAssignment = true)
        {
            descendant.AddAncestor(ancestor, generationalDistance, addRelationAssignment);
        }

        /// <summary>Assigns a Sim as a "sibling's descendant" to another Sim without knowing the ancestors of said "sibling's descendant."</summary>
        /// <param name="siblingOfAncestor">The alleged ancestor's sibling</param>
        /// <param name="descendantOfSibling">The alleged sibling's descendant</param>
        /// <param name="generationalDistance">The generational distance between the ancestor's sibling and said sibling's descendant, with 0 for aunts/uncles and nieces/nephews, 1 for great-aunts/uncles and great-nieces/nephews, etc.</param>
        /// <param name="addRelationAssignment">Determine whether to save the assignment as an instruction for rebuilding the relations later</param>
        public static void AddDescendantOfSibling(this Genealogy siblingOfAncestor, Genealogy descendantOfSibling, int generationalDistance = 0, bool addRelationAssignment = true)
        {
            descendantOfSibling.AddSiblingOfAncestor(siblingOfAncestor, generationalDistance, addRelationAssignment);
        }

        /// <summary>Assigns a Sim as an "ancestor's sibling" to another Sim without knowing the ancestors of the other Sim.</summary>
        /// <param name="descendantOfSibling">The alleged sibling's descendant</param>
        /// <param name="siblingOfAncestor">The alleged ancestor's sibling</param>
        /// <param name="generationalDistance">The generational distance between the ancestor's sibling and said sibling's descendant, with 0 for aunts/uncles and nieces/nephews, 1 for great-aunts/uncles and great-nieces/nephews, etc.</param>
        /// <param name="addRelationAssignment">Determine whether to save the assignment as an instruction for rebuilding the relations later</param>
        public static void AddSiblingOfAncestor(this Genealogy descendantOfSibling, Genealogy siblingOfAncestor, int generationalDistance = 0, bool addRelationAssignment = true)
        {
            int relationAssignmentIndex = -1;
            if (addRelationAssignment)
            {
                relationAssignmentIndex = RelationAssignments.FindIndex(relationAssignment => (string)relationAssignment[RelationAssignmentFieldNames.RelationType] == RelationTypeNames.DescendantOfSibling && relationAssignment[RelationAssignmentFieldNames.SimB] == descendantOfSibling);
                RelationAssignments.Insert(relationAssignmentIndex == -1 ? RelationAssignments.Count : relationAssignmentIndex, new Dictionary<string, object>
                    {
                        {
                            RelationAssignmentFieldNames.RelationType,
                            RelationTypeNames.DescendantOfSibling
                        },
                        {
                            RelationAssignmentFieldNames.GenerationalDistance,
                            generationalDistance
                        },
                        {
                            RelationAssignmentFieldNames.SimA,
                            descendantOfSibling
                        },
                        {
                            RelationAssignmentFieldNames.SimB,
                            siblingOfAncestor
                        }
                    });
            }
            siblingOfAncestor.AddCousin(descendantOfSibling, 0, generationalDistance + 1, true, false);
            foreach (GenealogyPlaceholder ancestor in siblingOfAncestor.GetGenealogyPlaceholder().Ancestors)
            {
                if (ancestor.Genealogy != null)
                {
                    descendantOfSibling.AddAncestor(ancestor.Genealogy, siblingOfAncestor.GetGenealogyPlaceholder().GetAncestorInfo(ancestor).GenerationalDistance + generationalDistance + 1, false);
                }
                foreach (GenealogyPlaceholder sibling in ancestor.Siblings)
                {
                    if (sibling.Genealogy != null)
                    {
                        descendantOfSibling.AddCousin(sibling.Genealogy, 0, siblingOfAncestor.GetGenealogyPlaceholder().GetAncestorInfo(ancestor).GenerationalDistance + generationalDistance + 2, false, false);
                    }
                }
            }
            if (relationAssignmentIndex > -1)
            {
                RebuildRelationAssignments();
            }
        }

        public static void ClearRelationsWith(this Genealogy self, Genealogy other)
        {
            RelationAssignments.RemoveAll(relationAssignment => relationAssignment[RelationAssignmentFieldNames.SimA] == self && relationAssignment[RelationAssignmentFieldNames.SimB] == other || relationAssignment[RelationAssignmentFieldNames.SimA] == other && relationAssignment[RelationAssignmentFieldNames.SimB] == self);
            RebuildRelationAssignments();
        }

        public static AncestorInfo GetAncestorInfo(this Genealogy descendant, Genealogy ancestor)
        {
            return descendant.GetGenealogyPlaceholder().GetAncestorInfo(ancestor.GetGenealogyPlaceholder());
        }

        public static AncestorInfo GetAncestorInfo(this GenealogyPlaceholder descendant, GenealogyPlaceholder ancestor)
        {
            List<AncestorInfo> ancestorInfoList = descendant.GetAncestorInfoList(ancestor);
            if (ancestorInfoList.Count == 0)
            {
                return null;
            }
            return ancestorInfoList[0];
        }

        public static List<AncestorInfo> GetAncestorInfoList(this Genealogy descendant, Genealogy ancestor)
        {
            return descendant.GetGenealogyPlaceholder().GetAncestorInfoList(ancestor.GetGenealogyPlaceholder());
        }

        public static List<AncestorInfo> GetAncestorInfoList(this GenealogyPlaceholder descendant, GenealogyPlaceholder ancestor)
        {
            List<AncestorInfo> ancestorInfoList = new List<AncestorInfo>(),
            cachedAncestorInfoList;
            if (descendant.CachedAncestorInfoLists.TryGetValue(ancestor, out cachedAncestorInfoList))
            {
                return cachedAncestorInfoList;
            }
            List<object[]> tempAncestorInfoAndParentList = new List<object[]>();
            foreach (GenealogyPlaceholder parent in descendant.Parents)
            {
                tempAncestorInfoAndParentList.Add(new object[]
                    {
                        new AncestorInfo(0, descendant),
                        parent
                    });
            }
            while (tempAncestorInfoAndParentList.Count > 0)
            {
                object[] tempAncestorInfoAndParent = tempAncestorInfoAndParentList[0];
                tempAncestorInfoAndParentList.RemoveAt(0);
                GenealogyPlaceholder tempParent = (GenealogyPlaceholder)tempAncestorInfoAndParent[1];
                if (tempParent == ancestor)
                {
                    ancestorInfoList.Add((AncestorInfo)tempAncestorInfoAndParent[0]);
                }
                else if (tempParent.IsAncestor(ancestor))
                {
                    foreach (GenealogyPlaceholder parent in tempParent.Parents)
                    {
                        tempAncestorInfoAndParentList.Add(new object[]
                            {
                                new AncestorInfo(((AncestorInfo)tempAncestorInfoAndParent[0]).GenerationalDistance + 1, tempParent),
                                parent
                            });
                    }
                }
            }
            ancestorInfoList.Sort((a, b) => a.GenerationalDistance == b.GenerationalDistance ? 0 : a.GenerationalDistance > b.GenerationalDistance ? 1 : -1);
            descendant.CachedAncestorInfoLists[ancestor] = ancestorInfoList;
            return ancestorInfoList;
        }

        public static float GetCoefficientOfRelationship(this Genealogy self, Genealogy other)
        {
            bool isSufficientlyRelatedToRuleOutRomance;
            return self.GetCoefficientOfRelationship(other, out isSufficientlyRelatedToRuleOutRomance);
        }

        public static DistantRelationInfo GetDistantRelationInfo(this Genealogy self, Genealogy other)
        {
            return self.GetGenealogyPlaceholder().GetDistantRelationInfo(other.GetGenealogyPlaceholder());
        }

        public static DistantRelationInfo GetDistantRelationInfo(this GenealogyPlaceholder self, GenealogyPlaceholder other)
        {
            List<DistantRelationInfo> distantRelationInfoList = self.GetDistantRelationInfoList(other);
            if (distantRelationInfoList.Count == 0)
            {
                return null;
            }
            return distantRelationInfoList[0];
        }

        public static List<DistantRelationInfo> GetDistantRelationInfoList(this Genealogy self, Genealogy other)
        {
            return self.GetGenealogyPlaceholder().GetDistantRelationInfoList(other.GetGenealogyPlaceholder());
        }

        public static List<DistantRelationInfo> GetDistantRelationInfoList(this GenealogyPlaceholder self, GenealogyPlaceholder other)
        {
            List<DistantRelationInfo> cachedDistantRelationInfoList,
            distantRelationInfoList = new List<DistantRelationInfo>();
            if (self.IsAncestor(other) || other.IsAncestor(self))
            {
                return distantRelationInfoList;
            }
            if (self.CachedDistantRelationInfoLists.TryGetValue(other, out cachedDistantRelationInfoList))
            {
                return cachedDistantRelationInfoList;
            }
            foreach (GenealogyPlaceholder ancestor1 in self.Ancestors)
            {
                foreach (GenealogyPlaceholder ancestor2 in other.Ancestors)
                {
                    AncestorInfo ancestor1Info = self.GetAncestorInfo(ancestor1),
                    ancestor2Info = other.GetAncestorInfo(ancestor2);
                    if (ancestor1.IsSibling(ancestor2))
                    {
                        if (ancestor1Info.GenerationalDistance < ancestor2Info.GenerationalDistance)
                        {
                            distantRelationInfoList.TryAddDistantRelationInfo(ancestor1Info.GenerationalDistance + 1, ancestor2Info.GenerationalDistance - ancestor1Info.GenerationalDistance, ancestor1, ancestor2, self);
                        }
                        else
                        {
                            distantRelationInfoList.TryAddDistantRelationInfo(ancestor2Info.GenerationalDistance + 1, ancestor1Info.GenerationalDistance - ancestor2Info.GenerationalDistance, ancestor1, ancestor2, other);
                        }
                    }
                }
            }
            distantRelationInfoList.Sort((a, b) =>
                {
                    int aCoRelPowAbs = 2 * a.Degree + a.TimesRemoved + (a.IsHalfRelative ? 2 : 1),
                    bCoRelPowAbs = 2 * b.Degree + b.TimesRemoved + (b.IsHalfRelative ? 2 : 1);
                    if (aCoRelPowAbs == bCoRelPowAbs && a.Degree == b.Degree && a.TimesRemoved == b.TimesRemoved)
                    {
                        return 0;
                    }
                    if (aCoRelPowAbs > bCoRelPowAbs || aCoRelPowAbs == bCoRelPowAbs && (a.Degree > b.Degree || a.Degree == b.Degree && a.TimesRemoved > b.TimesRemoved))
                    {
                        return 1;
                    }
                    return -1;
                });
            self.CachedDistantRelationInfoLists[other] = distantRelationInfoList;
            return distantRelationInfoList;
        }

        public static string GetMyFamilialDescriptionFor(this Genealogy self, Genealogy other)
        {
            string localizationKey = Common.kLocalizationPath + "/RelationNames",
            text = "";
            if (other == self)
            {
                return text;
            }
            if (Genealogy.IsParent(other, self))
            {
                text = Localization.LocalizeString(other.IMiniSimDescription.IsFemale, "Gameplay/Socializing:Parent");
            }
            else if (Genealogy.IsGrandparent(other, self))
            {
                text = Localization.LocalizeString(other.IMiniSimDescription.IsFemale, "Gameplay/Socializing:Grandparent");
            }
            else if (Genealogy.IsGreatGrandparent(other, self))
            {
                text = Localization.LocalizeString(other.IMiniSimDescription.IsFemale, "Gameplay/Socializing:GGP");
            }
            else if (self.GetGenealogyPlaceholder().IsAncestor(other))
            {
                text = Common.PlayerLanguage.GetAncestorString(other, self);
            }
            else if (Genealogy.IsChild(other, self))
            {
                text = Localization.LocalizeString(other.IMiniSimDescription.IsFemale, "Gameplay/Socializing:Child");
            }
            else if (Genealogy.IsGrandchild(other, self))
            {
                text = Localization.LocalizeString(other.IMiniSimDescription.IsFemale, "Gameplay/Socializing:Grandchild");
            }
            else if (Genealogy.IsGreatGrandchild(other, self))
            {
                text = Localization.LocalizeString(other.IMiniSimDescription.IsFemale, "Gameplay/Socializing:GGC");
            }
            else if (self.GetGenealogyPlaceholder().IsDescendant(other))
            {
                text = Common.PlayerLanguage.GetDescendantString(other, self);
            }
            else if (Genealogy.IsHalfSibling(other, self) && Tuning.kShowHalfRelatives && !Tuning.kShowHalfRelativesAsFullRelatives)
            {
                text = Localization.LocalizeString(other.IMiniSimDescription.IsFemale, "Gameplay/Socializing:HalfSibling");
            }
            else if (Genealogy.IsSibling(other, self) && (!Genealogy.IsHalfSibling(other, self) || Tuning.kShowHalfRelativesAsFullRelatives))
            {
                text = Localization.LocalizeString(other.IMiniSimDescription.IsFemale, "Gameplay/Socializing:Sibling");
            }
            else if (Genealogy.IsStepParent(other, self))
            {
                text = Localization.LocalizeString(other.IMiniSimDescription.IsFemale, "Gameplay/Socializing:StepParent");
            }
            else if (Genealogy.IsStepChild(other, self))
            {
                text = Localization.LocalizeString(other.IMiniSimDescription.IsFemale, "Gameplay/Socializing:StepChild");
            }
            else if (Genealogy.IsStepSibling(other, self))
            {
                text = Localization.LocalizeString(other.IMiniSimDescription.IsFemale, "Gameplay/Socializing:StepSibling");
            }
            else if (GenealogyExtended.IsHalfUncle(other, self) && Tuning.kShowHalfRelatives && !Tuning.kShowHalfRelativesAsFullRelatives)
            {
                text = !Common.PlayerLanguage.HasNthUncles || Tuning.kShow1stCousinsAsCousins ? Localization.LocalizeString(other.IMiniSimDescription.IsFemale, localizationKey + ":HalfUncle") : Localization.LocalizeString(other.IMiniSimDescription.IsFemale, localizationKey + ":NthHalfCousinNxRemovedUpward", "1", Localization.LocalizeString(other.IMiniSimDescription.IsFemale, localizationKey + ":OrdinalSuffixNoun1"), "", "");
            }
            else if (Genealogy.IsUncle(other, self) && (!GenealogyExtended.IsHalfUncle(other, self) || Tuning.kShowHalfRelativesAsFullRelatives))
            {
                text = !Common.PlayerLanguage.HasNthUncles || Tuning.kShow1stCousinsAsCousins ? Localization.LocalizeString(other.IMiniSimDescription.IsFemale, "Gameplay/Socializing:Uncle" + (Genealogy.IsMotherSideUncle(other, self) ? "MothersSide" : "")) : Localization.LocalizeString(other.IMiniSimDescription.IsFemale, localizationKey + ":NthCousinNxRemovedUpward", "1", Localization.LocalizeString(other.IMiniSimDescription.IsFemale, localizationKey + ":OrdinalSuffixNoun1"), "", "");
            }
            else if (Common.PlayerLanguage.TryGetSiblingOfAncestorString(other, self, out text))
            {
            }
            else if (GenealogyExtended.IsHalfNephew(other, self) && Tuning.kShowHalfRelatives && !Tuning.kShowHalfRelativesAsFullRelatives)
            {
                text = !Common.PlayerLanguage.HasNthUncles || Tuning.kShow1stCousinsAsCousins ? Localization.LocalizeString(other.IMiniSimDescription.IsFemale, localizationKey + ":HalfNephew") : Localization.LocalizeString(other.IMiniSimDescription.IsFemale, localizationKey + ":NthHalfCousinNxRemovedDownward", "1", Localization.LocalizeString(other.IMiniSimDescription.IsFemale, localizationKey + ":OrdinalSuffixNoun1"), "", "");
            }
            else if (Genealogy.IsNephew(other, self) && (!GenealogyExtended.IsHalfNephew(other, self) || Tuning.kShowHalfRelativesAsFullRelatives))
            {
                text = !Common.PlayerLanguage.HasNthUncles || Tuning.kShow1stCousinsAsCousins ? Localization.LocalizeString(other.IMiniSimDescription.IsFemale, "Gameplay/Socializing:Nephew") : Localization.LocalizeString(other.IMiniSimDescription.IsFemale, localizationKey + ":NthCousinNxRemovedDownward", "1", Localization.LocalizeString(other.IMiniSimDescription.IsFemale, localizationKey + ":OrdinalSuffixNoun1"), "", "");
            }
            else if (Common.PlayerLanguage.TryGetDescendantOfSiblingString(other, self, out text))
            {
            }
            else if (GenealogyExtended.IsHalfCousin(other, self) && Tuning.kShowHalfRelatives && !Tuning.kShowHalfRelativesAsFullRelatives && Tuning.kShow1stCousinsAsCousins)
            {
                text = Localization.LocalizeString(other.IMiniSimDescription.IsFemale, localizationKey + ":HalfCousin");
            }
            else if (Genealogy.IsCousin(other, self) && Tuning.kShow1stCousinsAsCousins && (!GenealogyExtended.IsHalfCousin(other, self) || Tuning.kShowHalfRelativesAsFullRelatives))
            {
                text = Localization.LocalizeString(other.IMiniSimDescription.IsFemale, "Gameplay/Socializing:Cousin");
            }
            else if (Common.PlayerLanguage.TryGetDistantRelationString(other, self, out text))
            {
            }
            else if (Genealogy.IsParentInLaw(other, self))
            {
                text = Localization.LocalizeString(other.IMiniSimDescription.IsFemale, "Gameplay/Socializing:ParentInLaw");
            }
            // Check if the selected Sim is married to one of the target's descendants.
            else if (self.Spouse != null && self.Spouse != other && self.Spouse.GetGenealogyPlaceholder().IsAncestor(other) && self.PartnerType == PartnerType.Marriage)
            {
                if (Genealogy.IsGrandparent(other, self.Spouse))
                {
                    text = Localization.LocalizeString(other.IMiniSimDescription.IsFemale, localizationKey + ":GrandparentInLaw");
                }
                else if (Genealogy.IsGreatGrandparent(other, self.Spouse))
                {
                    text = Localization.LocalizeString(other.IMiniSimDescription.IsFemale, localizationKey + ":GGPInLaw");
                }
                else
                {
                    text = Common.PlayerLanguage.GetAncestorString(other.IMiniSimDescription.IsFemale, other, self.Spouse, true);
                }
            }
            else if (Genealogy.IsChildInLaw(other, self))
            {
                text = Localization.LocalizeString(other.IMiniSimDescription.IsFemale, "Gameplay/Socializing:ChildInLaw");
            }
            // Check if the target is married to one of the selected Sim's descendants.
            else if (other.Spouse != null && other.Spouse != self && other.Spouse.GetGenealogyPlaceholder().IsAncestor(self) && other.PartnerType == PartnerType.Marriage)
            {
                if (Genealogy.IsGrandchild(other.Spouse, self))
                {
                    text = Localization.LocalizeString(other.IMiniSimDescription.IsFemale, localizationKey + ":GrandchildInLaw");
                }
                else if (Genealogy.IsGreatGrandchild(other.Spouse, self))
                {
                    text = Localization.LocalizeString(other.IMiniSimDescription.IsFemale, localizationKey + ":GGCInLaw");
                }
                else
                {
                    text = Common.PlayerLanguage.GetDescendantString(other.IMiniSimDescription.IsFemale, other.Spouse, self, true);
                }
            }
            else if (GenealogyExtended.IsHalfSiblingInLaw(other, self) && Tuning.kShowHalfRelatives && !Tuning.kShowHalfRelativesAsFullRelatives)
            {
                text = Localization.LocalizeString(other.IMiniSimDescription.IsFemale, localizationKey + ":HalfSiblingInLaw");
            }
            else if (Genealogy.IsSiblingInLaw(other, self) && (!GenealogyExtended.IsHalfSiblingInLaw(other, self) || Tuning.kShowHalfRelativesAsFullRelatives))
            {
                text = Localization.LocalizeString(other.IMiniSimDescription.IsFemale, "Gameplay/Socializing:SiblingInLaw");
            }
            if (other.IsFutureAncestorOf(self))
            {
                text = Localization.LocalizeString(other.IMiniSimDescription.IsFemale, "Gameplay/Socializing:Ancestor");
            }
            else if (other.IsFutureDescendantOf(self))
            {
                text = Localization.LocalizeString(other.IMiniSimDescription.IsFemale, "Gameplay/Socializing:Descendant");
            }
            if (self.SimDescription == null || other.SimDescription == null)
            {
                MiniSimDescription miniSimDescription = MiniSimDescription.Find(self.IMiniSimDescription.SimDescriptionId);
                if (miniSimDescription != null && miniSimDescription.MiniRelationships != null)
                {
                    foreach (MiniRelationship miniRelationship in miniSimDescription.MiniRelationships)
                    {
                        if (miniRelationship.SimDescriptionId == other.IMiniSimDescription.SimDescriptionId)
                        {
                            if (miniRelationship.CurrentLTR != LongTermRelationshipTypes.Spouse && (miniRelationship.InteractionBits & LongTermRelationship.InteractionBits.AreLitterMates) != 0)
                            {
                                text = Localization.LocalizeString(other.IMiniSimDescription.IsFemale, "Gameplay/Socializing:Sibling_Pet");
                            }
                            if ((miniRelationship.InteractionBits & LongTermRelationship.InteractionBits.HumanParentPetRel) != 0)
                            {
                                if (self.IMiniSimDescription.IsHuman)
                                {
                                    if (other.IMiniSimDescription.IsADogSpecies)
                                    {
                                        text = Localization.LocalizeString(other.IMiniSimDescription.IsFemale, "Gameplay/Socializing:Owns_puppy");
                                    }
                                    if (other.IMiniSimDescription.IsCat)
                                    {
                                        text = Localization.LocalizeString(other.IMiniSimDescription.IsFemale, "Gameplay/Socializing:Owns_kitten");
                                    }
                                }
                                else
                                {
                                    text = Localization.LocalizeString(other.IMiniSimDescription.IsFemale, "Gameplay/Socializing:Parent");
                                }
                            }
                            break;
                        }
                    }
                }
            }
            else
            {
                Relationship relationship = Relationship.Get(self.SimDescription, other.SimDescription, false);
                if (relationship != null)
                {
                    if (relationship.CurrentLTR != LongTermRelationshipTypes.Spouse && relationship.LTR.HasInteractionBit(LongTermRelationship.InteractionBits.AreLitterMates))
                    {
                        text = Localization.LocalizeString(other.SimDescription.IsFemale, "Gameplay/Socializing:Sibling_Pet");
                    }
                    if (relationship.LTR.HasInteractionBit(LongTermRelationship.InteractionBits.HumanParentPetRel))
                    {
                        if (self.IMiniSimDescription.IsHuman)
                        {
                            if (other.SimDescription.IsADogSpecies)
                            {
                                text = Localization.LocalizeString(other.SimDescription.IsFemale, "Gameplay/Socializing:Owns_puppy");
                            }
                            if (other.SimDescription.IsCat)
                            {
                                text = Localization.LocalizeString(other.SimDescription.IsFemale, "Gameplay/Socializing:Owns_kitten");
                            }
                        }
                        else
                        {
                            text = Localization.LocalizeString(other.SimDescription.IsFemale, "Gameplay/Socializing:Parent");
                        }
                    }
                }
            }
            if ((other.IMiniSimDescription.IsEP11Bot || self.IMiniSimDescription.IsEP11Bot || (other.IMiniSimDescription.IsFrankenstein || self.IMiniSimDescription.IsFrankenstein) && Tuning.kReplaceRelationsForSimBots) && !string.IsNullOrEmpty(text))
            {
                if (Genealogy.IsParent(other, self) && (self.IMiniSimDescription.IsEP11Bot || self.IMiniSimDescription.IsFrankenstein && Tuning.kReplaceRelationsForSimBots))
                {
                    text = Localization.LocalizeString(other.IMiniSimDescription.IsFemale, "Gameplay/Socializing:Creator");
                }
                else if (Genealogy.IsChild(other, self) && (other.IMiniSimDescription.IsEP11Bot || other.IMiniSimDescription.IsFrankenstein && Tuning.kReplaceRelationsForSimBots))
                {
                    text = Localization.LocalizeString(other.IMiniSimDescription.IsFemale, "Gameplay/Socializing:Creation");
                }
                else if ((self.IMiniSimDescription.IsEP11Bot || self.IMiniSimDescription.IsFrankenstein && Tuning.kReplaceRelationsForSimBots) && !(other.IMiniSimDescription.IsEP11Bot || other.IMiniSimDescription.IsFrankenstein && Tuning.kReplaceRelationsForSimBots))
                {
                    text = Localization.LocalizeString(other.IMiniSimDescription.IsFemale, "Gameplay/Socializing:FamilyMember");
                }
                else if (other.IMiniSimDescription.IsEP11Bot)
                {
                    text = Localization.LocalizeString(other.IMiniSimDescription.IsFemale, "Gameplay/Socializing:FamilyBot");
                }
                else if (other.IMiniSimDescription.IsFrankenstein && Tuning.kReplaceRelationsForSimBots)
                {
                    text = Localization.LocalizeString(other.IMiniSimDescription.IsFemale, localizationKey + ":FamilyBot");
                }
            }
            return text.Capitalize();
        }

        public static GenealogyPlaceholder GetGenealogyPlaceholder(this Genealogy self)
        {
            return GenealogyPlaceholder.GetGenealogyPlaceholder(self);
        }

        public static SiblingOfAncestorInfo GetSiblingOfAncestorInfo(this Genealogy descendantOfSibling, Genealogy siblingOfAncestor)
        {
            return descendantOfSibling.GetGenealogyPlaceholder().GetSiblingOfAncestorInfo(siblingOfAncestor.GetGenealogyPlaceholder());
        }

        public static SiblingOfAncestorInfo GetSiblingOfAncestorInfo(this GenealogyPlaceholder descendantOfSibling, GenealogyPlaceholder siblingOfAncestor)
        {
            List<SiblingOfAncestorInfo> siblingOfAncestorInfoList = descendantOfSibling.GetSiblingOfAncestorInfoList(siblingOfAncestor);
            if (siblingOfAncestorInfoList.Count == 0)
            {
                return null;
            }
            return siblingOfAncestorInfoList[0];
        }

        public static List<SiblingOfAncestorInfo> GetSiblingOfAncestorInfoList(this Genealogy descendantOfSibling, Genealogy siblingOfAncestor)
        {
            return descendantOfSibling.GetGenealogyPlaceholder().GetSiblingOfAncestorInfoList(siblingOfAncestor.GetGenealogyPlaceholder());
        }

        public static List<SiblingOfAncestorInfo> GetSiblingOfAncestorInfoList(this GenealogyPlaceholder descendantOfSibling, GenealogyPlaceholder siblingOfAncestor)
        {
            List<SiblingOfAncestorInfo> cachedSiblingOfAncestorInfoList,
            siblingOfAncestorInfoList = new List<SiblingOfAncestorInfo>();
            if (descendantOfSibling.CachedSiblingOfAncestorInfoLists.TryGetValue(siblingOfAncestor, out cachedSiblingOfAncestorInfoList))
            {
                return cachedSiblingOfAncestorInfoList;
            }
            foreach (GenealogyPlaceholder sibling in siblingOfAncestor.Siblings)
            {
                AncestorInfo ancestorInfo = descendantOfSibling.GetAncestorInfo(sibling);
                if (ancestorInfo != null)
                {
                    bool isHalfRelative = Genealogy.IsHalfSibling(sibling.Genealogy, siblingOfAncestor.Genealogy);
                    if (!isHalfRelative || Tuning.kShowHalfRelatives)
                    {
                        siblingOfAncestorInfoList.Add(new SiblingOfAncestorInfo(ancestorInfo.GenerationalDistance, isHalfRelative));
                    }
                }
            }
            siblingOfAncestorInfoList.Sort((a, b) =>
                {
                    if (a.GenerationalDistance > b.GenerationalDistance || a.GenerationalDistance == b.GenerationalDistance && a.IsHalfRelative && !b.IsHalfRelative)
                    {
                        return 1;
                    }
                    if (a.GenerationalDistance < b.GenerationalDistance || a.GenerationalDistance == b.GenerationalDistance && !a.IsHalfRelative && b.IsHalfRelative)
                    {
                        return -1;
                    }
                    return 0;
                });
            descendantOfSibling.CachedSiblingOfAncestorInfoLists[siblingOfAncestor] = siblingOfAncestorInfoList;
            return siblingOfAncestorInfoList;
        }

        public static bool IsCousin(this Genealogy sim1, Genealogy sim2, int degree = 1, int timesRemoved = 0, Genealogy closestDescendant = null)
        {
            foreach (DistantRelationInfo distantRelationInfo in sim1.GetDistantRelationInfoList(sim2))
            {
                if (distantRelationInfo.Degree == degree && distantRelationInfo.TimesRemoved == timesRemoved && (closestDescendant == null || distantRelationInfo.ClosestDescendant.Genealogy == closestDescendant))
                {
                    return true;
                }
            }
            return false;
        }

        public static bool IsFutureAncestorOf(this Genealogy ancestor, Genealogy descendant)
        {
            if (Sims3.Gameplay.TimeTravel.FutureDescendantService.sPersistableData != null)
            {
                foreach (Sims3.Gameplay.TimeTravel.FutureDescendantService.FutureDescendantHouseholdInfo householdInfo in Sims3.Gameplay.TimeTravel.FutureDescendantService.sPersistableData.ActiveDescendantHouseholdsInfo)
                {
                    if (householdInfo.mHouseholdMembers.Contains(descendant.IMiniSimDescription.SimDescriptionId))
                    {
                        if (householdInfo.IsSimAnAncestor(ancestor.IMiniSimDescription.SimDescriptionId))
                        {
                            return true;
                        }
                        foreach (ulong id in householdInfo.mAncestorsSimIds)
                        {
                            IMiniSimDescription sim = SimDescription.Find(id) as IMiniSimDescription ?? MiniSimDescription.Find(id);
                            Genealogy genealogy = sim.GetType().GetProperty("Genealogy").GetValue(sim, null) as Genealogy;
                            if (genealogy != null && genealogy.GetGenealogyPlaceholder().IsAncestor(ancestor.GetGenealogyPlaceholder()))
                            {
                                return true;
                            }
                        }
                    }
                }
            }
            return false;
        }

        public static bool IsFutureDescendantOf(this Genealogy descendant, Genealogy ancestor)
        {
            return ancestor.IsFutureAncestorOf(descendant);
        }

        public static bool IsHalfCousin(this Genealogy sim1, Genealogy sim2, int degree = 1, int timesRemoved = 0, Genealogy closestDescendant = null)
        {
            bool isOnlyHalf = false;
            foreach (DistantRelationInfo distantRelationInfo in sim1.GetDistantRelationInfoList(sim2))
            {
                if (distantRelationInfo.Degree == degree && distantRelationInfo.TimesRemoved == timesRemoved && (closestDescendant == null || distantRelationInfo.ClosestDescendant.Genealogy == closestDescendant))
                {
                    if (distantRelationInfo.IsHalfRelative)
                    {
                        isOnlyHalf = true;
                    }
                    else
                    {
                        return false;
                    }
                }
            }
            return isOnlyHalf;
        }

        public static bool IsHalfNephew(this Genealogy nephew, Genealogy uncle)
        {
            return IsHalfUncle(uncle, nephew);
        }

        public static bool IsHalfSiblingInLaw(this Genealogy sim1, Genealogy sim2)
        {
            if (sim1.Spouse != null && sim1.PartnerType == PartnerType.Marriage)
            {
                foreach (Genealogy sibling in sim1.Spouse.Siblings)
                {
                    if (sibling == sim2 || sibling.Spouse == sim2 && sibling.PartnerType == PartnerType.Marriage)
                    {
                        return Genealogy.IsHalfSibling(sim1.Spouse, sibling);
                    }
                }
            }
            if (sim2.Spouse != null && sim2.PartnerType == PartnerType.Marriage)
            {
                foreach (Genealogy sibling in sim2.Spouse.Siblings)
                {
                    if (sibling == sim1 || sibling.Spouse == sim1 && sibling.PartnerType == PartnerType.Marriage)
                    {
                        return Genealogy.IsHalfSibling(sim2.Spouse, sibling);
                    }
                }
            }
            return false;
        }

        public static bool IsHalfUncle(this Genealogy uncle, Genealogy nephew)
        {
            SiblingOfAncestorInfo siblingOfAncestorInfo = nephew.GetSiblingOfAncestorInfo(uncle);
            if (siblingOfAncestorInfo != null && siblingOfAncestorInfo.GenerationalDistance == 0 && siblingOfAncestorInfo.IsHalfRelative)
            {
                return true;
            }
            if (uncle.Spouse == null || uncle.PartnerType != PartnerType.Marriage)
            {
                return false;
            }
            siblingOfAncestorInfo = nephew.GetSiblingOfAncestorInfo(uncle.Spouse);
            return siblingOfAncestorInfo != null && siblingOfAncestorInfo.GenerationalDistance == 0 && siblingOfAncestorInfo.IsHalfRelative;
        }

        public static void RebuildRelationAssignments()
        {
            GenealogyPlaceholder.GenealogyPlaceholders.Clear();
            foreach (Dictionary<string, object> relationAssignment in RelationAssignments)
            {
                int generationalDistance = (int)relationAssignment[RelationAssignmentFieldNames.GenerationalDistance];
                Genealogy sim1 = (Genealogy)relationAssignment[RelationAssignmentFieldNames.SimA],
                sim2 = (Genealogy)relationAssignment[RelationAssignmentFieldNames.SimB];
                switch ((string)relationAssignment[RelationAssignmentFieldNames.RelationType])
                {
                    case RelationTypeNames.Cousin:
                        sim1.AddCousin(sim2, (int)relationAssignment[RelationAssignmentFieldNames.Degree], generationalDistance, (bool)relationAssignment[RelationAssignmentFieldNames.IsHigherUpFamilyTree], false);
                        break;
                    case RelationTypeNames.Descendant:
                        sim1.AddAncestor(sim2, generationalDistance, false);
                        break;
                    case RelationTypeNames.DescendantOfSibling:
                        sim1.AddSiblingOfAncestor(sim2, generationalDistance, false);
                        break;
                }
            }
        }
    }
}
