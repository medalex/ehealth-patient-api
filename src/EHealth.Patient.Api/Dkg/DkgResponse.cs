namespace EHealth.PatientApi.Dkg;

// mfssia wraps the UAL of a published Knowledge Asset either at the top level or under
// "data", depending on the endpoint: { "UAL": ... } or { "data": { "UAL": ... } }.
internal record DkgData(string? UAL);
internal record DkgResponse(string? UAL, DkgData? Data);
