using Destrospean.ExpandedGenealogy;
using Sims3.Gameplay.CAS;
using Sims3.Gameplay.Socializing;
using Sims3.Gameplay.Utilities;
using Sims3.UI.CAS;
using Tuning = Sims3.Gameplay.Destrospean.ExpandedGenealogy;

namespace Destrospean.Lang.ExpandedGenealogy
{
    public abstract class PlayerLanguage
    {
        public virtual bool HasNthUncles
        {
            get
            {
                return false;
            }
        }

        public string GetAncestorString(Genealogy ancestor, Genealogy descendant)
        {
            return GetAncestorString(ancestor.IMiniSimDescription.IsFemale, ancestor, descendant, false);
        }

        public virtual string GetAncestorString(bool isFemale, Genealogy ancestor, Genealogy descendant, bool isInLaw)
        {
            string greats = "";
            for (int i = 1; i < descendant.GetAncestorInfo(ancestor).GenerationalDistance; i++)
            {
                greats += Localization.LocalizeString(isFemale, Common.kLocalizationPath + "/RelationNames:Great");
            }
            return Localization.LocalizeString(isFemale, Common.kLocalizationPath + "/RelationNames:GreatNxGrandparent" + (isInLaw ? "InLaw" : ""), greats);
        }

        public virtual string GetDescendantOfSiblingString(bool isFemale, Genealogy descendantOfSibling, Genealogy siblingOfAncestor)
        {
            SiblingOfAncestorInfo siblingOfAncestorInfo = descendantOfSibling.GetSiblingOfAncestorInfo(siblingOfAncestor);
            if (siblingOfAncestorInfo == null)
            {
                return "";
            }
            string greats = "";
            for (int i = 1; i < siblingOfAncestorInfo.GenerationalDistance; i++)
            {
                greats += Localization.LocalizeString(isFemale, Common.kLocalizationPath + "/RelationNames:Great");
            }
            return Localization.LocalizeString(isFemale, siblingOfAncestorInfo.IsHalfRelative && !Tuning.kShowHalfRelativesAsFullRelatives ? Common.kLocalizationPath + "/RelationNames:GreatNxHalfNephew" : Common.kLocalizationPath + "/RelationNames:GreatNxNephew", greats);
        }

        public string GetDescendantString(Genealogy descendant, Genealogy ancestor)
        {
            return GetDescendantString(descendant.IMiniSimDescription.IsFemale, descendant, ancestor, false);
        }

        public virtual string GetDescendantString(bool isFemale, Genealogy descendant, Genealogy ancestor, bool isInLaw)
        {
            string greats = "";
            for (int i = 1; i < descendant.GetAncestorInfo(ancestor).GenerationalDistance; i++)
            {
                greats += Localization.LocalizeString(isFemale, Common.kLocalizationPath + "/RelationNames:Great");
            }
            return Localization.LocalizeString(isFemale, Common.kLocalizationPath + "/RelationNames:GreatNxGrandchild" + (isInLaw ? "InLaw" : ""), greats);
        }

        public abstract string GetDistantRelationString(bool isFemale, Genealogy sim, DistantRelationInfo distantRelationInfo);

        public string GetDistantRelationString(Genealogy sim, DistantRelationInfo distantRelationInfo)
        {
            return GetDistantRelationString(sim.IMiniSimDescription.IsFemale, sim, distantRelationInfo);
        }

        public virtual string GetNthUncleDegreeString(int degree)
        {
            string text = (degree + (HasNthUncles ? 1 : 0)).ToString();
            return text + Localization.LocalizeString(Common.kLocalizationPath + "/RelationNames:OrdinalSuffixNoun" + GetOrdinalSuffix(text));
        }

        public virtual string GetOrdinalSuffix(string number)
        {
            if (number.Length > 1)
            {
                switch (number.Substring(number.Length - 2))
                {
                    case "11":
                        return "11";
                    case "12":
                        return "12";
                }
            }
            return number.Substring(number.Length - 1);
        }

        public virtual string GetSiblingOfAncestorString(bool isFemale, Genealogy siblingOfAncestor, Genealogy descendantOfSibling)
        {
            SiblingOfAncestorInfo siblingOfAncestorInfo = descendantOfSibling.GetSiblingOfAncestorInfo(siblingOfAncestor);
            if (siblingOfAncestorInfo == null)
            {
                return "";
            }
            string greats = "";
            for (int i = 1; i < siblingOfAncestorInfo.GenerationalDistance; i++)
            {
                greats += Localization.LocalizeString(isFemale, Common.kLocalizationPath + "/RelationNames:Great");
            }
            return Localization.LocalizeString(isFemale, siblingOfAncestorInfo.IsHalfRelative && !Tuning.kShowHalfRelativesAsFullRelatives ? Common.kLocalizationPath + "/RelationNames:GreatNxHalfUncle" : Common.kLocalizationPath + "/RelationNames:GreatNxUncle", greats);
        }

        public bool TryGetDescendantOfSiblingString(Genealogy descendantOfSibling, Genealogy siblingOfAncestor, out string result)
        {
            string text = GetDescendantOfSiblingString(descendantOfSibling.IMiniSimDescription.IsFemale, descendantOfSibling, siblingOfAncestor);
            if (string.IsNullOrEmpty(text) && siblingOfAncestor.Spouse != null && siblingOfAncestor.Spouse != descendantOfSibling && siblingOfAncestor.PartnerType == PartnerType.Marriage)
            {
                text = GetDescendantOfSiblingString(descendantOfSibling.IMiniSimDescription.IsFemale, descendantOfSibling, siblingOfAncestor.Spouse);
            }
            result = text;
            return !string.IsNullOrEmpty(result);
        }

        public bool TryGetDescendantOfSiblingString(SimDescription descendantOfSibling, SimDescription siblingOfAncestor, out string result)
        {
            return TryGetDescendantOfSiblingString(descendantOfSibling.Genealogy, siblingOfAncestor.Genealogy, out result);
        }

        public bool TryGetDistantRelationString(Genealogy simWithRelationName, Genealogy simToGetRelationTo, out string result)
        {
            result = GetDistantRelationString(simWithRelationName, simWithRelationName.GetDistantRelationInfo(simToGetRelationTo));
            return !string.IsNullOrEmpty(result);
        }

        public bool TryGetDistantRelationString(SimDescription simWithRelationName, SimDescription simToGetRelationTo, out string result)
        {
            /* DistantRelationInfo distantRelationInfo = simWithRelationName.Genealogy.CalculateDistantRelation(simToGetRelationTo.Genealogy);
             * if (distantRelationInfo == null)
             * {    
             *     if (simWithRelationName.Genealogy.Spouse != null && simWithRelationName.Genealogy.Spouse != simToGetRelationTo.Genealogy && simWithRelationName.Genealogy.PartnerType == PartnerType.Marriage)
             *     {
             *         distantRelationInfo = simWithRelationName.Genealogy.Spouse.CalculateDistantRelation(simToGetRelationTo.Genealogy);
             *     }
             *     if (distantRelationInfo == null)
             *     {
             *         if (simToGetRelationTo.Genealogy.Spouse != null && simToGetRelationTo.Genealogy.Spouse != simWithRelationName.Genealogy && simToGetRelationTo.Genealogy.PartnerType == PartnerType.Marriage)
             *         {
             *             distantRelationInfo = simWithRelationName.Genealogy.CalculateDistantRelation(simToGetRelationTo.Genealogy.Spouse);
             *         }
             *         if (distantRelationInfo != null)
             *         {
             *             text = GetDistantRelationString(simWithRelationName.Genealogy, distantRelationInfo);
             *         }
             *     }
             *     else
             *     {
             *         text = GetDistantRelationString(simWithRelationName.IsFemale, simWithRelationName.Genealogy.Spouse, distantRelationInfo);
             *     }
             * }
             * else
             * {
             *     text = GetDistantRelationString(simWithRelationName.Genealogy, distantRelationInfo);
             * }
             */
            return TryGetDistantRelationString(simWithRelationName.Genealogy, simToGetRelationTo.Genealogy, out result);
        }

        public bool TryGetSiblingOfAncestorString(Genealogy siblingOfAncestor, Genealogy descendantOfSibling, out string result)
        {
            string text = GetSiblingOfAncestorString(siblingOfAncestor.IMiniSimDescription.IsFemale, siblingOfAncestor, descendantOfSibling);
            if (string.IsNullOrEmpty(text) && siblingOfAncestor.Spouse != null && descendantOfSibling != siblingOfAncestor.Spouse && siblingOfAncestor.PartnerType == PartnerType.Marriage)
            {
                text = GetSiblingOfAncestorString(siblingOfAncestor.IMiniSimDescription.IsFemale, siblingOfAncestor.Spouse, descendantOfSibling);
            }
            result = text;
            return !string.IsNullOrEmpty(result);
        }

        public bool TryGetSiblingOfAncestorString(SimDescription siblingOfAncestor, SimDescription descendantOfSibling, out string result)
        {
            return TryGetSiblingOfAncestorString(siblingOfAncestor.Genealogy, descendantOfSibling.Genealogy, out result);
        }
    }
}
