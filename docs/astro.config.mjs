import { defineConfig } from 'astro/config';
import starlight from '@astrojs/starlight';

// https://astro.build/config
export default defineConfig({
	site: 'https://omegametor.github.io',
  	base: '/GS2ML/',
	integrations: [
		starlight({
			title: 'GS2ML Documentation',
			social: {
				github: 'https://github.com/OmegaMetor/GS2ML',
			},
			editLink: {
				baseUrl: "https://github.com/OmegaMetor/GS2ML/edit/main/docs/"
			},
			sidebar: [
				{
					label: 'Guides',
					autogenerate: { directory: '' }
				}
			],
		}),
	],

	// Process images with sharp: https://docs.astro.build/en/guides/assets/#using-sharp
	image: { service: { entrypoint: 'astro/assets/services/sharp' } },
});
