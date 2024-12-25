public partial class ViewModel : Component
{
	public Weapon Weapon { get; set; }

	[Property] public SkinnedModelRenderer Arms { get; set; }
	[Property] public SkinnedModelRenderer ModelRenderer { get; set; }

	Player Player => Weapon.Player;

	[Property, Group( "References" )] public GameObject Muzzle { get; set; }

	public void SetVisible( bool visible )
	{
		ModelRenderer.Enabled = visible;
		Arms.Enabled = visible;
	}

	protected override void OnStart()
	{
		ModelRenderer?.Set( "b_deploy", true );
		if ( !Network.IsOwner ) GameObject.Enabled = false;
	}

	void ApplyAnimationTransform()
	{
		if ( !Network.IsOwner ) return;
		if ( !ModelRenderer.IsValid() ) return;
		if ( !ModelRenderer.Enabled ) return;

		var bone = ModelRenderer.SceneModel.GetBoneLocalTransform( "camera" );

		var scale = 1f; // TODO: View Bob Setting

		localPosition += bone.Position * scale;
		localRotation *= bone.Rotation * scale;
	}

	private Vector3 lerpedWishLook;
	private Vector3 localPosition;
	private Rotation localRotation;

	private Vector3 lerpedLocalPosition;
	private Rotation lerpedLocalRotation;

	protected override void OnUpdate()
	{
		localRotation = Rotation.Identity;
		localPosition = Vector3.Zero;

		ApplyAnimationTransform();
		lerpedLocalRotation = Rotation.Lerp( lerpedLocalRotation, localRotation, Time.Delta * 10f );
		lerpedLocalPosition = lerpedLocalPosition.LerpTo( localPosition, Time.Delta * 10f );

		LocalRotation = lerpedLocalRotation;
		LocalPosition = lerpedLocalPosition;
	}

}
