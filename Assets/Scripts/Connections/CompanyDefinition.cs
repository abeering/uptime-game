using System;
using UnityEngine;

namespace Uptime.Connections
{
    [Serializable]
    public sealed class CompanySpeaker
    {
        public string id;
        public string displayName;
        public string role;
    }

    // One authored company for the current Connections scenarios. This is content,
    // not a run generator or a registry of claimed connection identities.
    [CreateAssetMenu(menuName = "Uptime/Connections/Company Definition")]
    public sealed class CompanyDefinition : ScriptableObject
    {
        [SerializeField] private string companyId;
        [SerializeField] private string displayName;
        [SerializeField] private CompanySpeaker customerSupport;
        [SerializeField] private CompanySpeaker operations;
        [SerializeField] private string[] orderSubmitKillResponses;
        [SerializeField] private string[] genericBenignKillResponses;

        public string CompanyId => companyId;
        public string DisplayName => displayName;
        public CompanySpeaker CustomerSupport => customerSupport;
        public CompanySpeaker Operations => operations;
        public string[] OrderSubmitKillResponses => orderSubmitKillResponses;
        public string[] GenericBenignKillResponses => genericBenignKillResponses;
    }
}
