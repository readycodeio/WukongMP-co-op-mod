using b1;
using ReadyM.SDK.Client.Mapping;
using WukongMp.Sdk.Common.Archetypes.Mixins;

namespace WukongMp.Coop.Mapping;

public class CoopMappings : IShapeMappings
{
    public void Register(IShapeMappingRegistry registry)
    {
        registry.For<MainCharacterData, BUS_IntervalTriggerImpl>()
            .Map(
                MainCharacterData.Field.BeguilingChantEligible,
                (ref eligible, trigger) => eligible = trigger.CurrentState is BUS_IntervalTriggerImpl.IntervalTriggerEnableState
            );
    }
}