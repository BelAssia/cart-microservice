# Etape 1 : Image de build
# On utilise l'image officielle .NET SDK 8.0 pour compiler l'application
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
# Definit le repertoire de travail dans le conteneur
WORKDIR /src

# Copier uniquement le fichier de projet pour restaurer les dependances NuGet
COPY ["CartAPI.csproj", "./"]
# Restaurer les packages NuGet necessaires au projet
RUN dotnet restore

# Copier tout le code source du projet dans le conteneur
COPY . .

# Compiler et publier le projet en mode Release
# Les fichiers publies seront dans /app/publish
RUN dotnet publish -c Release -o /app/publish

# Etape 2 : Image finale (runtime)
# On utilise l'image officielle ASP.NET 8.0, plus légère, pour exécuter l'application
FROM mcr.microsoft.com/dotnet/aspnet:8.0
# Definir le répertoire de travail pour l'execution
WORKDIR /app
# Copier les fichiers publies de l'etape de build vers l'image finale
COPY --from=build /app/publish .

# Exposer le port 8080 pour le conteneur
EXPOSE 8080
# Configurer ASP.NET pour ecouter toutes les IP sur le port 8080
ENV ASPNETCORE_URLS=http://+:8080

# Definir la commande a executer au demarrage du conteneur
# Lancer l'API .NET avec le fichier CartAPI.dll
ENTRYPOINT ["dotnet", "CartAPI.dll"]