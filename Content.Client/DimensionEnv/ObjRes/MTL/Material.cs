using Robust.Client.Graphics;

namespace Content.Client.DimensionEnv.ObjRes.MTL;

public record struct Material(Vector3 Ka,Vector3 Kd, Vector3 Ke, int Illum,Vector3 Ks,int Ns, int D, int Tr,Texture? MapKa,Texture? MapKd);