# Запускать от имени администратора.
# Добавляет локальную маску schedulecollege.local в hosts.
# IP нужно заменить на IP компьютера, где запущен ScheduleCollege.

$serverIp = Read-Host "Введите IP компьютера-сервера, например 192.168.1.25"
$line = "$serverIp schedulecollege.local"
$hosts = "$env:SystemRoot\System32\drivers\etc\hosts"

if ((Get-Content $hosts) -notcontains $line) {
    Add-Content -Path $hosts -Value $line
    Write-Host "Добавлено: $line"
} else {
    Write-Host "Такая запись уже есть: $line"
}
