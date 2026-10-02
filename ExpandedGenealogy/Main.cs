using System;
using System.Reflection;
using Sims3.Gameplay.Actors;
using Sims3.Gameplay.CAS;
using Sims3.Gameplay.EventSystem;
using Sims3.Gameplay.Socializing;
using Sims3.SimIFace;

namespace Destrospean.ExpandedGenealogy
{
    public class Main
    {
        [Tunable]
        protected static bool kInstantiator;

        static Main()
        {
            EventListener simInstantiatedListener = null;
            World.sOnWorldLoadFinishedEventHandler += (sender, e) =>
                {
                    foreach (Sim sim in Sims3.Gameplay.Queries.GetObjects<Sim>())
                    {
                        AddInteractions(sim);
                        if (sim.Genealogy != null)
                        {
                            MiniSimDescription miniSimDescription = MiniSimDescription.Find(sim.SimDescription.SimDescriptionId);
                            if (miniSimDescription != null && miniSimDescription.MiniRelationships != null)
                            {
                                foreach (MiniRelationship miniRelationship in miniSimDescription.MiniRelationships)
                                {
                                    MiniSimDescription otherMiniSimDescription = MiniSimDescription.Find(miniRelationship.GetOtherSimDescriptionId(miniSimDescription));
                                    if (otherMiniSimDescription != null && otherMiniSimDescription.Genealogy != null)
                                    {
                                        miniRelationship.FamilialString = sim.Genealogy.GetMyFamilialDescriptionFor(otherMiniSimDescription.Genealogy);
                                        if (!string.IsNullOrEmpty(miniRelationship.FamilialString))
                                        {
                                            miniRelationship.Entry = miniRelationship.FamilialString;
                                        }
                                    }
                                }
                            }
                        }

                    }
                    if (Household.ActiveHousehold != null)
                    {
                        GenealogyExtended.RebuildRelationAssignments();
                    }
                    simInstantiatedListener = EventTracker.AddListener(EventTypeId.kSimInstantiated, evt =>
                        {
                            try
                            {
                                Sim sim = evt.TargetObject as Sim;
                                if (sim != null)
                                {
                                    AddInteractions(sim);
                                    if (sim.Genealogy != null)
                                    {
                                        sim.Genealogy.GetGenealogyPlaceholder();
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                ((IScriptErrorWindow)AppDomain.CurrentDomain.GetData("ScriptErrorWindow")).DisplayScriptError(null, ex);
                            }
                            return ListenerAction.Keep;
                        });
                };
            World.sOnWorldQuitEventHandler += (sender, e) =>
                {
                    GenealogyPlaceholder.GenealogyPlaceholders.Clear();
                    EventTracker.RemoveListener(simInstantiatedListener);
                    simInstantiatedListener = null;
                };
            Common.ReplaceMethod(typeof(Genealogy).GetMethod("AddChild"), typeof(Replacements).GetMethod("AddChild"));
            Common.ReplaceMethod(typeof(Genealogy).GetMethod("ClearDerivedData", BindingFlags.NonPublic | BindingFlags.Instance), typeof(Replacements).GetMethod("ClearDerivedData"));
            Common.ReplaceMethod(typeof(Genealogy).GetMethod("IsBloodRelated", BindingFlags.Public | BindingFlags.Instance), typeof(Replacements).GetMethod("IsBloodRelated"));
            Common.ReplaceMethod(typeof(Genealogy).GetMethod("IsCousin"), typeof(Replacements).GetMethod("IsCousin"));
            Common.ReplaceMethod(typeof(Genealogy).GetMethod("IsFutureBloodRelated"), typeof(Replacements).GetMethod("IsFutureBloodRelated"));
            Common.ReplaceMethod(typeof(Genealogy).GetMethod("IsGrandparent"), typeof(Replacements).GetMethod("IsGrandparent"));
            Common.ReplaceMethod(typeof(Genealogy).GetMethod("IsGreatGrandparent"), typeof(Replacements).GetMethod("IsGreatGrandparent"));
            Common.ReplaceMethod(typeof(Genealogy).GetMethod("IsHalfSibling", BindingFlags.Public | BindingFlags.Static), typeof(Replacements).GetMethod("IsHalfSibling"));
            Common.ReplaceMethod(typeof(Genealogy).GetMethod("IsSiblingInLaw"), typeof(Replacements).GetMethod("IsSiblingInLaw"));
            Common.ReplaceMethod(typeof(Genealogy).GetMethod("IsStepRelated"), typeof(Replacements).GetMethod("IsStepRelated"));
            Common.ReplaceMethod(typeof(Genealogy).GetMethod("IsUncle"), typeof(Replacements).GetMethod("IsUncle"));
            Common.ReplaceMethod(typeof(Sims3.Gameplay.UI.HudModel).GetMethod("GetLTRRelationshipString"), typeof(Replacements).GetMethod("GetLTRRelationshipString"));
            Common.ReplaceMethod(typeof(SimDescription).GetMethod("GetMyFamilialDescriptionFor"), typeof(Replacements).GetMethod("GetMyFamilialDescriptionFor"));
            Common.ReplaceMethod(typeof(SimDescription).GetMethod("MakeUniqueId"), typeof(Replacements).GetMethod("MakeUniqueId"));
            Type nraasWoohooerRelationshipsType = Type.GetType("NRaas.CommonSpace.Helpers.Relationships, NRaasWoohooer");
            if (nraasWoohooerRelationshipsType != null)
            {
                Common.ReplaceMethod(nraasWoohooerRelationshipsType.GetMethod("IsCloselyRelated", new[]
                    {
                        typeof(SimDescription),
                        typeof(SimDescription),
                        typeof(bool)
                    }), typeof(Replacements).GetMethod("IsCloselyRelated"));
            }
        }

        static void AddInteractions(Sim sim)
        {
            if (sim != null)
            {
                sim.AddInteraction(Interactions.AssignRelation.Singleton, true);
                sim.AddInteraction(Interactions.ClearRelations.Singleton, true);
            }
        }
    }
}
