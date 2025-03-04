﻿namespace BlazorEcommerce.Application.Specifications.Identity;
public class IdentityCodeSpecification : BaseSpecifications<IdentityCode>
{
	public IdentityCodeSpecification(string email, bool forRegistrationConfirmed = true)
	{
		WhereCriteria = p => p.Email == email && p.ForRegistrationConfirmed == forRegistrationConfirmed;

		OrderBy = p => p.CreationTime;
	}
}