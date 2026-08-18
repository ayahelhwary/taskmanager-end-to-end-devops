# Task Manager API — المرحلة 1: التطبيق الأساسي

ده جزء من مشروع DevOps متكامل. المرحلة دي بس هدفها: API شغال محليًا متصل بـ PostgreSQL.
المراحل الجاية (Docker, Kubernetes, Terraform, CI/CD, Monitoring) هتتضاف فوق الأساس ده.

## المتطلبات
- .NET 8 SDK
- Docker (لتشغيل PostgreSQL محليًا بسهولة)

## خطوات التشغيل

### 1. تحققي إن .NET SDK متثبت
```bash
dotnet --version
```
لو الأمر مش موجود، نزلي .NET 8 SDK من:
https://dotnet.microsoft.com/download/dotnet/8.0

### 2. شغّلي قاعدة البيانات محليًا
```bash
docker compose -f docker-compose.db-only.yml up -d
```
لو الأمر ده فشل، يبقى Docker مش متثبت أو مش شغال — نزّلي Docker Desktop من:
https://www.docker.com/products/docker-desktop

### 3. رجّعي الحزم (restore packages)
```bash
dotnet restore
```

### 4. أنشئي أول migration وطبقيها على الداتابيز
```bash
dotnet tool install --global dotnet-ef
dotnet ef migrations add InitialCreate
dotnet ef database update
```

### 5. شغّلي التطبيق
```bash
dotnet run
```

هيفتح على حاجة زي `https://localhost:5001` أو `http://localhost:5000`.
افتحي `/swagger` عشان تجربي الـendpoints من واجهة تفاعلية.

## الـEndpoints المتاحة
| Method | Path              | الوظيفة                  |
|--------|-------------------|---------------------------|
| GET    | /api/tasks        | جلب كل المهام             |
| GET    | /api/tasks/{id}   | جلب مهمة واحدة            |
| POST   | /api/tasks        | إضافة مهمة جديدة          |
| PUT    | /api/tasks/{id}   | تعديل مهمة                |
| DELETE | /api/tasks/{id}   | حذف مهمة                  |
| GET    | /health/live      | Liveness check            |
| GET    | /health/ready      | Readiness (بيتأكد من الداتابيز) |

## ليه فيه health checks من الأول؟
لأن المرحلة الجاية هي Kubernetes، وK8s محتاج يعرف يفرّق بين:
- **Liveness**: التطبيق شغال ولا لأ (لو مش شغال، K8s بيعمل restart للـpod)
- **Readiness**: التطبيق جاهز يستقبل traffic ولا لأ (زي لما الاتصال بالداتابيز لسه بيتحقق منه)

الفصل ده بين الاتنين معمول بقصد عشان يظهر فهمك للموضوع في الانترفيو.
