# ADR 0002: Statically registered feature modules

Status: Accepted

Each feature publishes stable tool identifiers, descriptors and dependency
registrations through `IFeatureModule`. Modules are compiled into the
application and may depend on Application and Core, but not on one another.
Runtime loading of third-party DLLs is deferred until APIs and a security model
are mature.
