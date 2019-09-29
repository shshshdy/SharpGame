using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGame
{
    public static partial class glm
    {

        public static bool intersectRayPlane(in vec3 orig, in vec3 dir, in vec3 planeOrig, in vec3 planeNormal, out float intersectionDistance)
        {
            float d = dot(dir, planeNormal);
            float Epsilon = glm.epsilon;

            if (d < -Epsilon)
            {
                intersectionDistance = dot(planeOrig - orig, planeNormal) / d;
                return true;
            }
            intersectionDistance = 0.0f;
            return false;
        }


        public static bool intersectRayTriangle(in vec3 orig, in vec3 dir, in vec3 vert0, in vec3 vert1,
            in vec3 vert2, ref vec2 baryPosition, ref float distance)
        {
            // find vectors for two edges sharing vert0
            vec3 edge1 = vert1 - vert0;
            vec3 edge2 = vert2 - vert0;

            // begin calculating determinant - also used to calculate U parameter
            vec3 p = cross(dir, edge2);

            // if determinant is near zero, ray lies in plane of triangle
            var det = dot(edge1, p);

            vec3 qvec;

            if (det > glm.epsilon)
            {
                // calculate distance from vert0 to ray origin
                vec3 tvec = orig - vert0;

                // calculate U parameter and test bounds
                baryPosition.x = dot(tvec, p);

                if (baryPosition.x < (0) || baryPosition.x > det)
                    return false;

                // prepare to test V parameter
                qvec = cross(tvec, edge1);

                // calculate V parameter and test bounds
                baryPosition.y = dot(dir, qvec);
                if ((baryPosition.y < (0)) || ((baryPosition.x + baryPosition.y) > det))
                    return false;
            }
            else if (det < -glm.epsilon)
            {
                // calculate distance from vert0 to ray origin
                vec3 tvec = orig - vert0;

                // calculate U parameter and test bounds
                baryPosition.x = dot(tvec, p);
                if ((baryPosition.x > (0)) || (baryPosition.x < det))
                    return false;

                // prepare to test V parameter
                qvec = cross(tvec, edge1);

                // calculate V parameter and test bounds
                baryPosition.y = dot(dir, qvec);
                if ((baryPosition.y > (0)) || (baryPosition.x + baryPosition.y < det))
                    return false;
            }
            else
            {
                return false; // ray is parallel to the plane of the triangle
            }

            float inv_det = (1) / det;

            // calculate distance, ray intersects triangle
            distance = dot(edge2, qvec) * inv_det;
            baryPosition *= inv_det;

            return true;
        }


        public static bool intersectLineTriangle
        (
            in vec3 orig, in vec3 dir,
            in vec3 vert0, in vec3 vert1, in vec3 vert2,
            ref vec3 position
        )
        {
            var Epsilon = glm.epsilon;

            vec3 edge1 = vert1 - vert0;
            vec3 edge2 = vert2 - vert0;

            vec3 pvec = cross(dir, edge2);

            float det = dot(edge1, pvec);

            if (det > -Epsilon && det < Epsilon)
                return false;
            float inv_det = (1) / det;

            vec3 tvec = orig - vert0;

            position.y = dot(tvec, pvec) * inv_det;
            if (position.y < (0) || position.y > (1))
                return false;

            vec3 qvec = cross(tvec, edge1);

            position.z = dot(dir, qvec) * inv_det;
            if (position.z < (0) || position.y + position.z > (1))
                return false;

            position.x = dot(edge2, qvec) * inv_det;

            return true;
        }


        public static bool intersectRaySphere
        (
            in vec3 rayStarting, in vec3 rayNormalizedDirection,
            in vec3 sphereCenter, float sphereRadiusSquered,
            out float intersectionDistance
        )
        {
            float Epsilon = glm.epsilon;
            vec3 diff = sphereCenter - rayStarting;
            float t0 = dot(diff, rayNormalizedDirection);
            float dSquared = dot(diff, diff) - t0 * t0;
            if (dSquared > sphereRadiusSquered)
            {
                intersectionDistance = 0.0f;
                return false;
            }

            float t1 = sqrt(sphereRadiusSquered - dSquared);
            intersectionDistance = t0 > t1 + Epsilon ? t0 - t1 : t0 + t1;
            return intersectionDistance > Epsilon;
        }

        public static bool intersectRaySphere
        (
            in vec3 rayStarting, in vec3 rayNormalizedDirection,
            in vec3 sphereCenter, float sphereRadius,
            ref vec3 intersectionPosition, ref vec3 intersectionNormal
        )
        {
            float distance;
            if (intersectRaySphere(rayStarting, rayNormalizedDirection, sphereCenter, sphereRadius * sphereRadius, out distance))
            {
                intersectionPosition = rayStarting + rayNormalizedDirection * distance;
                intersectionNormal = (intersectionPosition - sphereCenter) / sphereRadius;
                return true;
            }
            return false;
        }


        public static bool intersectLineSphere
        (
            in vec3 point0, in vec3 point1,
            in vec3 sphereCenter, float sphereRadius,
            ref vec3 intersectionPoint1, ref vec3 intersectionNormal1,
            ref vec3 intersectionPoint2, ref vec3 intersectionNormal2
        )
        {
            float Epsilon = glm.epsilon;
            vec3 dir = normalize(point1 - point0);
            vec3 diff = sphereCenter - point0;
            float t0 = dot(diff, dir);
            float dSquared = dot(diff, diff) - t0 * t0;
            if (dSquared > sphereRadius * sphereRadius)
            {
                return false;
            }
            float t1 = sqrt(sphereRadius * sphereRadius - dSquared);
            if (t0 < t1 + Epsilon)
                t1 = -t1;
            intersectionPoint1 = point0 + dir * (t0 - t1);
            intersectionNormal1 = (intersectionPoint1 - sphereCenter) / sphereRadius;
            intersectionPoint2 = point0 + dir * (t0 + t1);
            intersectionNormal2 = (intersectionPoint2 - sphereCenter) / sphereRadius;
            return true;
        }
    }
}
