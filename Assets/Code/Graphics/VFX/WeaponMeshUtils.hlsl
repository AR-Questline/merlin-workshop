#pragma once

bool IsKandra(in uint linkedWeaponSource)
{
    return (linkedWeaponSource & 1 << 1) > 0;
}

bool IsDrake(in uint linkedWeaponSource)
{
    return (linkedWeaponSource & 1 << 0) > 0;
}
