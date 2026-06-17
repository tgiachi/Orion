namespace OrionIrcd.Core.Data.Internal;

public record ServiceRegistrationObject(Type ServiceType, Type ImplementationType, int Priority = 0);
