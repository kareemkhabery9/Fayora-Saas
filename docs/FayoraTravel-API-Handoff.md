# FayoraTravel SaaS API Handoff

## قواعد عامة

- كل المسارات تستخدم JSON، ما لم تكن عملية رفع ملف (`multipart/form-data`).
- المسارات العامة لا تحتاج token.
- مسارات الشركة تحتاج JWT يحمل دور `Company`، ولا يمكنها الوصول إلا لبيانات شركتها.
- مسارات الإدارة العليا تحتاج JWT يحمل دور `SuperAdmin`.
- الحالات والقيم الـ enum تقبل بصيغة النص، مثل `Confirmed` و`Accommodation`.

## Company Authentication وOnboarding

Base: `/api/company-auth`

- `POST /register/email`
- `POST /login/email`
- `POST /login/google`

Base: `/api/company`

- `GET /profile`, `PUT /profile`
- `GET /slug/availability`, `PUT /slug`
- `PUT /theme`
- `PUT /payment-methods`
- `POST /onboarding/complete`

ملف الشركة يتضمن أرقام WhatsApp. وسائل الدفع تحفظ رقم/معرّف الحساب، ورابط QR لـ InstaPay إن وُجد.

## Company Inventory

### Packages

Base: `/api/company/packages`

- `POST /`, `GET /`, `GET /{packageId}`, `PUT /{packageId}`, `DELETE /{packageId}`
- `PATCH /{packageId}/visibility`
- `PUT /{packageId}/accommodations`
- إدارة مواعيد الرحلات تحت `/{packageId}/occurrences`.

### Accommodations

Base: `/api/company/accommodations`

- `POST /`, `GET /`, `GET /{accommodationId}`, `PUT /{accommodationId}`, `DELETE /{accommodationId}`
- `PATCH /{accommodationId}/visibility`
- القائمة تدعم `status=Active|Suspended` وPagination.

## Public Storefront

Base: `/api/public/companies/{slug}`

- `GET /storefront`
- `GET /packages`, `GET /packages/{packageId}`
- `GET /packages/{packageId}/checkout-quote`
- `GET /accommodations`, `GET /accommodations/{accommodationId}`

لا تظهر إلا الشركة المكتملة وغير المعلقة، والخدمات الظاهرة فقط.

## Manual Payment Bookings

ارفع الإيصال أولًا:

- `POST /api/public/payment-receipts` كـ `multipart/form-data` باسم `file`.

إنشاء حجز رحلة:

- `POST /api/public/companies/{slug}/packages/{packageId}/occurrences/{occurrenceId}/bookings`

إنشاء حجز إقامة:

- `POST /api/public/companies/{slug}/accommodations/{accommodationId}/bookings`

الإقامة تحسب عدد الليالي × سعر الليلة، وتتحقق من السعة وتداخل التواريخ. الإقامة الآن دفع كامل فقط. الرحلة تدعم العربون إن حددت الشركة نسبته في الباقة.

## Booking Tracking وQR

- `POST /api/public/bookings/track` مع `bookingReference` و`email`.
- تتبع الحجز يعرض التفاصيل وQR بعد تأكيد الشركة فقط.
- تأكيد الشركة يرسل إيميل للعميل يحتوي المرجع وQR.
- لا يوجد إلغاء أو Refund تلقائي من المنصة؛ سياسة الإلغاء معلومات ظاهرة في الباقة فقط.

## Company Dashboard

Base: `/api/company/dashboard`

- `GET /home`
- `GET /bookings` يدعم: `status`, `isDepositPayment`, `serviceType`, `serviceId`, `search`, `fromDate`, `toDate`, `page`, `pageSize`.
- `GET /bookings/status-summary`
- `GET /bookings/{bookingId}`
- `GET /bookings/export` بنفس فلاتر القائمة ويعيد ملف Excel.

إجراءات حجز الرحلة:

- `PATCH /api/company/package-bookings/{bookingId}/confirm`
- `PATCH /api/company/package-bookings/{bookingId}/reject`
- `POST /api/company/package-bookings/scan-qr`

إجراءات حجز الإقامة:

- `PATCH /api/company/accommodation-bookings/{bookingId}/confirm`
- `PATCH /api/company/accommodation-bookings/{bookingId}/reject`
- `POST /api/company/accommodation-bookings/scan-qr`

## Reviews

عام:

- `POST /api/public/companies/{slug}/reviews/{targetType}/{targetId}`
- `GET /api/public/companies/{slug}/reviews/{targetType}/{targetId}`

`targetType` هو `TravelPackage` أو `HousingUnit`. التقييم يبدأ `Pending`، والواجهة العامة ترى `Approved` فقط.

للشركة:

- `GET /api/company/reviews` مع `status`, `targetType`, `targetName`, Pagination.
- `PATCH /api/company/reviews/{reviewId}/approve`
- `PATCH /api/company/reviews/{reviewId}/reject`

## Super Admin Dashboard

Base: `/api/super-admin/companies`

- `GET /` مع بحث، حالة، Pagination.
- `GET /{companyId}`
- `PATCH /{companyId}/activate`
- `PATCH /{companyId}/suspend`
- `GET /dashboard/home` مع `fromDate`, `toDate`.
- `GET /dashboard/bookings` مع فلترة الشركة والحالة والنوع والتاريخ وPagination.
- `GET /{companyId}/services` مع `serviceType` وPagination.

Super Admin يشاهد التقييمات ضمن الإحصاءات فقط ولا يملك endpoint لتعديلها أو حذفها.

## قيود حماية مفعلة

- JWT role authorization وعزل بيانات كل شركة.
- QR موقّع، صالح لفترة محدودة، مربوط بالشركة، ومسح مرة واحدة فقط.
- Rate limit لعمليات الدخول والحجز العام ورفع الإيصالات والتتبع والتقييمات.
- رفع الإيصالات: JPG/JPEG/PNG حتى 5 MB.
