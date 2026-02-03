# EALDS Frontend - Folder Structure

## Overview
This document describes the folder structure for the Enterprise Asset Lifecycle & Depreciation System (EALDS) frontend application.

## Root Structure

```
src/
├── api/                    # API client configuration and interceptors
├── assets/                 # Static assets (images, icons, etc.)
├── config/                 # Application configuration files
├── modules/                # Business domain modules (feature-based)
├── routes/                  # Route definitions and route guards
├── shared/                  # Shared/common code across modules
├── stores/                  # Global state management (Zustand stores)
├── App.tsx                  # Root component
├── main.tsx                 # Application entry point
└── index.css               # Global styles
```

## Modules Structure

Each module follows a consistent structure:

```
modules/
├── auth/                   # Authentication module
│   ├── components/         # Auth-specific components (LoginForm, etc.)
│   ├── hooks/              # Auth-specific hooks (useAuth, etc.)
│   ├── pages/              # Auth pages (Login, ForgotPassword, ResetPassword)
│   ├── services/           # Auth API services
│   ├── types/              # Auth TypeScript types/interfaces
│   └── utils/              # Auth utility functions
│
├── assets/                 # Asset management module
│   ├── components/          # Asset components (AssetList, AssetForm, etc.)
│   ├── hooks/              # Asset hooks (useAssets, useAssetDetails, etc.)
│   ├── pages/              # Asset pages (AssetList, AssetCreate, AssetEdit, etc.)
│   ├── services/           # Asset API services
│   ├── types/              # Asset TypeScript types
│   └── utils/              # Asset utility functions
│
├── approvals/               # Approval workflow module
│   ├── components/         # Approval components
│   ├── hooks/              # Approval hooks
│   ├── pages/              # Approval pages
│   ├── services/           # Approval API services
│   ├── types/              # Approval TypeScript types
│   └── utils/              # Approval utility functions
│
├── maintenance/             # Maintenance module
│   ├── components/         # Maintenance components
│   ├── hooks/              # Maintenance hooks
│   ├── pages/              # Maintenance pages
│   ├── services/           # Maintenance API services
│   ├── types/              # Maintenance TypeScript types
│   └── utils/              # Maintenance utility functions
│
└── depreciation/            # Depreciation module
    ├── components/         # Depreciation components
    ├── hooks/              # Depreciation hooks
    ├── pages/              # Depreciation pages
    ├── services/           # Depreciation API services
    ├── types/              # Depreciation TypeScript types
    └── utils/              # Depreciation utility functions
```

## Shared Structure

```
shared/
├── components/             # Reusable UI components (Button, Table, Form, etc.)
├── constants/              # Application constants (routes, enums, etc.)
├── hooks/                  # Shared hooks (useLocalStorage, useDebounce, etc.)
├── layouts/                # Layout components (MainLayout, Sidebar, Header, etc.)
├── services/               # Shared services (HTTP client, error handling, etc.)
├── types/                  # Shared TypeScript types (User, ApiResponse, etc.)
└── utils/                  # Shared utility functions (formatters, validators, etc.)
```

## Directory Purposes

### `/api`
- API client setup (Axios configuration)
- Request/response interceptors
- API error handling

### `/config`
- Environment variables
- Feature flags
- Application settings

### `/routes`
- Route definitions
- Protected route components
- Route guards

### `/stores`
- Zustand store definitions
- Global state management
- Store slices for different domains

### Module Structure (`/modules/{domain}`)

#### `components/`
- Domain-specific React components
- Not shared across other modules
- Example: `AssetList.tsx`, `AssetForm.tsx`

#### `hooks/`
- Custom React hooks for domain logic
- Example: `useAssets.ts`, `useAssetDetails.ts`

#### `pages/`
- Full page components (route-level components)
- Example: `AssetListPage.tsx`, `AssetCreatePage.tsx`

#### `services/`
- API service functions
- Data fetching and mutations
- Example: `assetService.ts`, `assetApi.ts`

#### `types/`
- TypeScript type definitions
- Domain-specific interfaces and types
- Example: `asset.types.ts`, `asset.interfaces.ts`

#### `utils/`
- Domain-specific utility functions
- Helpers and formatters
- Example: `assetHelpers.ts`, `assetFormatters.ts`

## Naming Conventions

- **Components**: PascalCase (e.g., `AssetList.tsx`, `LoginForm.tsx`)
- **Hooks**: camelCase starting with "use" (e.g., `useAssets.ts`, `useAuth.ts`)
- **Services**: camelCase ending with "Service" or "Api" (e.g., `assetService.ts`, `authApi.ts`)
- **Types**: camelCase ending with "types" or "interfaces" (e.g., `asset.types.ts`, `user.interfaces.ts`)
- **Utils**: camelCase ending with "Utils" or "Helpers" (e.g., `dateUtils.ts`, `formHelpers.ts`)
- **Pages**: PascalCase ending with "Page" (e.g., `AssetListPage.tsx`, `LoginPage.tsx`)

## Principles

1. **Domain-Driven**: Code is organized by business domain, not by technical layer
2. **Self-Contained**: Each module contains all its related code (components, hooks, services, types)
3. **Shared Code**: Only truly reusable code goes in `/shared`
4. **Scalability**: Easy to add new modules following the same structure
5. **Clarity**: Clear separation of concerns and easy navigation
