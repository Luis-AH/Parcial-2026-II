# Etapa de compilación
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copiar el archivo del proyecto y restaurar las dependencias
COPY ["PlataformaIncidencias.csproj", "./"]
RUN dotnet restore "./PlataformaIncidencias.csproj"

# Copiar el resto del código fuente y compilar
COPY . .
RUN dotnet publish "PlataformaIncidencias.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Etapa de ejecución
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# Exponer el puerto configurado por Render
EXPOSE 80

# Iniciar la aplicación
ENTRYPOINT ["dotnet", "PlataformaIncidencias.dll"]
