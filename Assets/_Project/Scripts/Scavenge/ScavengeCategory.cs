namespace BlackCommission.Scavenge
{
    /// <summary>
    /// Item content taxonomy (scavenging quick-spec §Item Categories — 12 for launch).
    /// Drives visual silhouette and the settlement note pools; NOT a value signal (all
    /// items share the neutral aged-earth palette per spec). Lives in the content layer,
    /// not the pure-logic Scavenge.Core.
    /// </summary>
    public enum ScavengeCategory
    {
        PersonalCorrespondence,
        FamilyPhotography,
        ChildrensArtifacts,
        MedicalPharmaceutical,
        CivicDocuments,
        CulturalPublications,
        PersonalEffects,
        HouseholdTechnology,
        ProfessionalTools,
        NativePlantSpecimens,
        ReligiousCeremonial,
        ResidentialFixtures,
    }
}
