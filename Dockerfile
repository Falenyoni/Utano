FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project files first — Docker caches this layer until a .csproj changes
COPY src/Utano.API/Utano.API.csproj                                                   src/Utano.API/
COPY src/Shared/Utano.Module.Core/Utano.Module.Core.csproj                            src/Shared/Utano.Module.Core/
COPY src/Shared/Utano.Shared.Models/Utano.Shared.Models.csproj                        src/Shared/Utano.Shared.Models/
COPY src/Modules/Utano.Module.Identity/Utano.Module.Identity.csproj                   src/Modules/Utano.Module.Identity/
COPY src/Modules/Utano.Module.Patients/Utano.Module.Patients.csproj                   src/Modules/Utano.Module.Patients/
COPY src/Modules/Utano.Module.Appointments/Utano.Module.Appointments.csproj           src/Modules/Utano.Module.Appointments/
COPY src/Modules/Utano.Module.Billing/Utano.Module.Billing.csproj                     src/Modules/Utano.Module.Billing/
COPY src/Modules/Utano.Module.ClinicalNotes/Utano.Module.ClinicalNotes.csproj         src/Modules/Utano.Module.ClinicalNotes/
COPY src/Modules/Utano.Module.Doctors/Utano.Module.Doctors.csproj                     src/Modules/Utano.Module.Doctors/
COPY src/Modules/Utano.Module.Inventory/Utano.Module.Inventory.csproj                 src/Modules/Utano.Module.Inventory/

RUN dotnet restore src/Utano.API/Utano.API.csproj

# Copy all source and publish
COPY . .
RUN dotnet publish src/Utano.API/Utano.API.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

# Override these via environment variables or docker-compose
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

EXPOSE 8080
ENTRYPOINT ["dotnet", "Utano.API.dll"]
