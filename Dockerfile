# Sử dụng base image của .NET 8 SDK để build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy file sln và các file csproj để restore dependencies (tối ưu cache Docker)
COPY ["NoteVui.sln", "./"]
COPY ["NoteVui.API/NoteVui.API.csproj", "NoteVui.API/"]
COPY ["NoteVui.Application/NoteVui.Application.csproj", "NoteVui.Application/"]
COPY ["NoteVui.Domain/NoteVui.Domain.csproj", "NoteVui.Domain/"]
COPY ["NoteVui.Infrastructure/NoteVui.Infrastructure.csproj", "NoteVui.Infrastructure/"]

# Chạy restore (tải các thư viện NuGet)
RUN dotnet restore "NoteVui.sln"

# Copy toàn bộ mã nguồn còn lại vào
COPY . .

# Build và Publish ra file dll
WORKDIR "/src/NoteVui.API"
RUN dotnet publish "NoteVui.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# ---------------------------------------------------
# Giai đoạn 2: Tạo Image môi trường Runtime (siêu nhẹ)
# ---------------------------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Expose cổng mặc định của .NET 8 (thường là 8080)
EXPOSE 8080

# Chép các bundle đã publish từ phase build sang
COPY --from=build /app/publish .

# Định nghĩa lệnh chạy khi khởi động container
ENTRYPOINT ["dotnet", "NoteVui.API.dll"]
