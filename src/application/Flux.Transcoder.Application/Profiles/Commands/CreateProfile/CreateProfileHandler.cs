using Flux.Transcoder.Application.Abstractions;
using Flux.Transcoder.Domain.Common;

namespace Flux.Transcoder.Application.Profiles.Commands.CreateProfile;

public class CreateProfileHandler
{
    public static async Task<CreateProfileResult> HandleAsync(
        CreateProfileCommand command,
        IRepository<EncodingProfile> profiles,
        IUnitOfWork unitOfWork,
        CancellationToken cancellationToken)
    {
        var renditions = command.Renditions
            .Select(r => new Rendition(r.Width, r.Height, r.BitrateKbps, r.Codec));

        var profile = new EncodingProfile(command.Name, renditions);

        await profiles.AddAsync(profile, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new CreateProfileResult(profile.Id);
    }
}