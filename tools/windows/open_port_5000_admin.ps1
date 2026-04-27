# Запускать от имени администратора.
# Открывает входящий TCP-порт 5000 для доступа к ScheduleCollege из локальной сети.

netsh advfirewall firewall add rule name="ScheduleCollege 5000" dir=in action=allow protocol=TCP localport=5000

Write-Host "Готово. Порт 5000 открыт для входящих подключений."
