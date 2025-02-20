#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build
WORKDIR /app

WORKDIR /src
COPY ["SmICSWebApp/SmICSWebApp.csproj", "SmICSWebApp/"]
COPY ["SmICSCoreLib/SmICSCoreLib.csproj", "SmICSCoreLib/"]
RUN dotnet restore "SmICSWebApp/SmICSWebApp.csproj"

WORKDIR /src/.
COPY . .
RUN dotnet build "SmICSWebApp/SmICSWebApp.csproj" -c Release -o /app/build

FROM build AS publish
COPY . ./
RUN dotnet publish "SmICSWebApp/SmICSWebApp.csproj" -c Release -o /app/out

FROM mcr.microsoft.com/dotnet/aspnet:5.0 AS final
WORKDIR /app

COPY --from=publish /app/out .

COPY Certificates/ /usr/local/share/ca-certificates/
RUN find /usr/local/share/ca-certificates -type f -exec chmod 644 {} \;
RUN update-ca-certificates

COPY RKIAlgorithm/Statistik.dod.zip RKIAlgorithm/Statistik.dod.zip
COPY RKIAlgorithm/Statistik.dod.tar.gz RKIAlgorithm/Statistik.dod.tar.gz
COPY SmICSWebApp/Resources/RRuntime/R_Script_00010.R Resources/RRuntime/
COPY pub/jranke.asc jranke.asc
COPY pub/jranke2.asc jranke2.asc
COPY SmICSWebApp/Resources/ Resources/

RUN apt-get update 
Run apt-get -y upgrade
RUN apt-get -y install ca-certificates wget gnupg2 gnupg1 software-properties-common apt-transport-https
RUN apt-get -y install dirmngr --install-recommends

RUN apt-key add jranke.asc
RUN apt-key add jranke2.asc

RUN add-apt-repository 'deb http://cloud.r-project.org/bin/linux/debian buster-cran40/'
RUN apt-get update
RUN apt-get -y install -t buster-cran40 r-base

RUN Rscript -e "options(repos = 'https://cran.r-project.org')"
RUN Rscript -e "install.packages('RJSONIO')"
RUN Rscript -e "install.packages('surveillance')"
RUN Rscript -e "install.packages('dplyr')"
RUN Rscript -e "install.packages('lubridate')"
RUN Rscript -e "install.packages('plotrix')"
RUN Rscript -e "install.packages('/app/RKIAlgorithm/Statistik.dod.tar.gz', repos=NULL, type='source')"

EXPOSE 80
EXPOSE 443
ENV SMICS_VISU_PORT=3231

ENTRYPOINT ["dotnet", "SmICSWebApp.dll"]
