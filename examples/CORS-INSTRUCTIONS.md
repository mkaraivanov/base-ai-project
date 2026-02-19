# CORS Configuration

After every backend rebuild, ensure CORS (Cross-Origin Resource Sharing) is correctly configured for all API endpoints. This is critical for both development and production environments to allow or restrict cross-origin requests as required by your application.

## Steps to Ensure CORS is Configured

1. **Check CORS Middleware**: Verify that CORS middleware is enabled in your backend server (e.g., ASP.NET Core, Express.js, etc.).
2. **Allowed Origins**: Update the list of allowed origins as needed for your frontend and any external integrations.
3. **Allowed Methods and Headers**: Ensure all required HTTP methods and headers are permitted.
4. **Environment Variables**: Use environment variables to manage CORS settings securely and flexibly.
5. **Test CORS**: After each rebuild, test API endpoints from the frontend and external tools (like Postman) to confirm CORS headers are present and correct.

## Example (ASP.NET Core)

```csharp
// In Program.cs or Startup.cs
app.UseCors(policy =>
    policy.WithOrigins("https://your-frontend.com")
          .AllowAnyHeader()
          .AllowAnyMethod()
);
```

## Example (Node.js/Express)

```js
const cors = require('cors');
app.use(cors({
  origin: process.env.ALLOWED_ORIGINS?.split(',') || '*',
  methods: ['GET', 'POST', 'PUT', 'DELETE'],
  credentials: true
}));
```

---

**Always verify CORS configuration after every backend rebuild to prevent integration issues.**
