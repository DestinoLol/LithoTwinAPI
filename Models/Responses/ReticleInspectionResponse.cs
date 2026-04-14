using LithoTwinAPI.Models;

namespace LithoTwinAPI.Models.Responses;

public record ReticleInspectionResponse(Reticle Reticle, string? Warning);