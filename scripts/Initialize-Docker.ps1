#Requires -Version 7.0
param([switch]$TrustCertificate)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$certificateDirectory = Join-Path $root '.docker-local'
$environmentFile = Join-Path $root '.env'
$pfx = Join-Path $certificateDirectory 'bookverse.pfx'
$crt = Join-Path $certificateDirectory 'bookverse.crt'

if (!(Test-Path -LiteralPath $environmentFile)) {
    if ((Test-Path -LiteralPath $pfx) -or (Test-Path -LiteralPath $crt)) {
        throw 'Existing certificates found without .env. Restore the matching .env before continuing.'
    }
    New-Item -ItemType Directory -Path $certificateDirectory -Force | Out-Null
    $certificatePassword = [Convert]::ToHexString([Security.Cryptography.RandomNumberGenerator]::GetBytes(24))
    $sqlPassword = 'Bv1!' + [Convert]::ToHexString([Security.Cryptography.RandomNumberGenerator]::GetBytes(24))
    $jwtKey = [Convert]::ToBase64String([Security.Cryptography.RandomNumberGenerator]::GetBytes(64))
    $rsa = [Security.Cryptography.RSA]::Create(2048)
    try {
        $request = [Security.Cryptography.X509Certificates.CertificateRequest]::new(
            'CN=localhost', $rsa, [Security.Cryptography.HashAlgorithmName]::SHA256,
            [Security.Cryptography.RSASignaturePadding]::Pkcs1)
        $names = [Security.Cryptography.X509Certificates.SubjectAlternativeNameBuilder]::new()
        $names.AddDnsName('localhost')
        $names.AddDnsName('api')
        $request.CertificateExtensions.Add($names.Build())
        $now = [DateTimeOffset]::UtcNow
        $certificate = $request.CreateSelfSigned($now.AddMinutes(-5), $now.AddYears(1))
        try {
            [IO.File]::WriteAllBytes($pfx, $certificate.Export('Pfx', $certificatePassword))
            [IO.File]::WriteAllText($crt, $certificate.ExportCertificatePem())
            [IO.File]::WriteAllLines($environmentFile, @(
                "BOOKVERSE_CERT_PASSWORD=$certificatePassword",
                "BOOKVERSE_SQL_PASSWORD=$sqlPassword",
                "BOOKVERSE_JWT_KEY=$jwtKey"
            ))
        }
        finally { $certificate.Dispose() }
    }
    finally { $rsa.Dispose() }
}

if (!(Test-Path -LiteralPath $pfx) -or !(Test-Path -LiteralPath $crt) -or
    !(Select-String -LiteralPath $environmentFile -Pattern '^BOOKVERSE_CERT_PASSWORD=' -Quiet)) {
    throw 'Docker configuration is incomplete. Check .env and .docker-local for a matching configuration.'
}

if ($TrustCertificate) {
    $certificate = [Security.Cryptography.X509Certificates.X509Certificate2]::CreateFromPemFile($crt)
    $store = [Security.Cryptography.X509Certificates.X509Store]::new('Root', 'CurrentUser')
    try {
        $store.Open([Security.Cryptography.X509Certificates.OpenFlags]::ReadWrite)
        $store.Add($certificate)
    }
    finally {
        $store.Dispose()
        $certificate.Dispose()
    }
}

Write-Host 'Docker configuration is ready. Run docker compose up --build from the repository root.'
