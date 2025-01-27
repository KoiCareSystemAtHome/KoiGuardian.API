using KoiGuardian.Core.Repository;
using KoiGuardian.DataAccess.Db;

namespace KoiGuardian.Api.Services;

public interface ISymptomService
{
    Task<List<Symptom>> DiseaseTypePredict(List<Symptom> symptoms);
    Task<List<Symptom>> GetByType(SymptomType? type);
}

public class SymptomService( 
    IRepository<Symptom> symptomRepository
    ) : ISymptomService
{
    public Task<List<Symptom>> DiseaseTypePredict(List<Symptom> symptoms)
    {
        throw new NotImplementedException();
    }

    public async Task<List<Symptom>> GetByType(SymptomType? type)
    {
        if (type == null) type = SymptomType.Common;
        return (await symptomRepository
            .FindAsync( u => u.Type == type, CancellationToken.None)).ToList();
    }


}