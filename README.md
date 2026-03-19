# Современные технологии разработки программного обеспечения
### Вариант 13 Медицинский пациент

### Лабораторная работа №1
<details>
<summary>1.	«Кэширование» - Реализация сервиса генерации контрактов, кэширование его ответов</summary>
<br> 
  
В рамках первой лабораторной работы необходимо:
* Реализовать сервис генерации контрактов на основе Bogus,
* Реализовать кеширование при помощи IDistributedCache и Redis,
* Реализовать структурное логирование сервиса генерации,
* Настроить оркестрацию Aspire. 
  
</details>

Реализовано:
1. Доменная модель: *Domain*
2. Сервис генерации данных пациента с заданным идентификатором: *GenerationService*
3. Сервис кэширования Redis: *CachingService*
4. Сервис оркестрации Aspire: *AppHost.AppHost*
5. Сервис телеметрии OpenTelemetry: *AppHost.ServiceDefaults*

Примеры интерфейса:

![1](https://github.com/user-attachments/assets/bc050486-c80b-4a8b-87e2-6fd0b510a68f)

![2](https://github.com/user-attachments/assets/c8bdd235-9656-476f-ae93-f8a9e7a75bd2)

![3](https://github.com/user-attachments/assets/bb429a0c-7553-4c5c-b863-4bacf5c5b67a)

### Лабораторная работа №2
<details>
<summary>2. «Балансировка нагрузки» - Реализация апи гейтвея, настройка его работы</summary>
<br>
  
В рамках второй лабораторной работы необходимо:
* Настроить оркестрацию на запуск нескольких реплик сервиса генерации,
* Реализовать апи гейтвей на основе Ocelot,
* Имплементировать алгоритм балансировки нагрузки согласно варианту.
</details>

Реализовано:
1. ApiGateway: *ApiGateway*
2. Балансировщик нагрузки WeightedRandomBalancer: *ApiGateway/LoadBalancer*

Примеры интерфейса: 
![4](https://github.com/user-attachments/assets/469da2a1-2431-4faa-9e25-0cb7596a3352)

![5](https://github.com/user-attachments/assets/26bbde9a-8b7c-458b-9835-b1a933d47df5)


